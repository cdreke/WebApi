﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Annotations;
using Microsoft.Data.Edm.Library;

namespace System.Web.Http.OData.Builder
{
    /// <summary>
    /// <see cref="EdmTypeBuilder"/> builds <see cref="IEdmType"/>'s from <see cref=" StructuralTypeConfiguration"/>'s.
    /// </summary>
    internal class EdmTypeBuilder
    {
        private readonly List<StructuralTypeConfiguration> _configurations;
        private readonly Dictionary<Type, IEdmStructuredType> _types = new Dictionary<Type, IEdmStructuredType>();
        private readonly List<IEdmDirectValueAnnotationBinding> _directValueAnnotations = new List<IEdmDirectValueAnnotationBinding>();
        private readonly Dictionary<PropertyInfo, IEdmProperty> _properties = new Dictionary<PropertyInfo, IEdmProperty>();

        internal EdmTypeBuilder(IEnumerable<StructuralTypeConfiguration> configurations)
        {
            _configurations = configurations.ToList();
        }

        private Dictionary<Type, IEdmStructuredType> GetEdmTypes()
        {
            // Reset
            _types.Clear();
            _directValueAnnotations.Clear();
            _properties.Clear();

            // Create headers to allow CreateEdmTypeBody to blindly references other things.
            foreach (StructuralTypeConfiguration config in _configurations)
            {
                CreateEdmTypeHeader(config);
            }

            foreach (StructuralTypeConfiguration config in _configurations)
            {
                CreateEdmTypeBody(config);
            }

            foreach (EntityTypeConfiguration entity in _configurations.OfType<EntityTypeConfiguration>())
            {
                CreateNavigationProperty(entity);
            }

            return _types;
        }

        private void CreateEdmTypeHeader(StructuralTypeConfiguration config)
        {
            if (!_types.ContainsKey(config.ClrType))
            {
                if (config.Kind == EdmTypeKind.Complex)
                {
                    _types.Add(config.ClrType, new EdmComplexType(config.Namespace, config.Name));
                }
                else
                {
                    EntityTypeConfiguration entity = config as EntityTypeConfiguration;
                    Contract.Assert(entity != null);

                    IEdmEntityType baseType = null;

                    if (entity.BaseType != null)
                    {
                        CreateEdmTypeHeader(entity.BaseType);
                        baseType = _types[entity.BaseType.ClrType] as IEdmEntityType;

                        Contract.Assert(baseType != null);
                    }

                    _types.Add(config.ClrType, new EdmEntityType(config.Namespace, config.Name, baseType, entity.IsAbstract ?? false, isOpen: false));
                }
            }
        }

        private void CreateEdmTypeBody(StructuralTypeConfiguration config)
        {
            IEdmType edmType = _types[config.ClrType];

            if (edmType.TypeKind == EdmTypeKind.Complex)
            {
                CreateComplexTypeBody(edmType as EdmComplexType, config as ComplexTypeConfiguration);
            }
            else
            {
                if (edmType.TypeKind == EdmTypeKind.Entity)
                {
                    CreateEntityTypeBody(edmType as EdmEntityType, config as EntityTypeConfiguration);
                }
            }
        }

        private void CreateStructuralTypeBody(EdmStructuredType type, StructuralTypeConfiguration config)
        {
            foreach (PropertyConfiguration property in config.Properties)
            {
                IEdmProperty edmProperty = null;

                switch (property.Kind)
                {
                    case PropertyKind.Primitive:
                        PrimitivePropertyConfiguration primitiveProperty = property as PrimitivePropertyConfiguration;
                        CreatePrimitiveProperty(primitiveProperty, type, config, out edmProperty);
                        break;

                    case PropertyKind.Complex:
                        ComplexPropertyConfiguration complexProperty = property as ComplexPropertyConfiguration;
                        IEdmComplexType complexType = _types[complexProperty.RelatedClrType] as IEdmComplexType;

                        edmProperty = type.AddStructuralProperty(
                            complexProperty.PropertyInfo.Name,
                            new EdmComplexTypeReference(complexType, complexProperty.OptionalProperty));
                        break;

                    case PropertyKind.Collection:
                        CollectionPropertyConfiguration collectionProperty = property as CollectionPropertyConfiguration;
                        IEdmTypeReference elementTypeReference = null;
                        if (_types.ContainsKey(collectionProperty.ElementType))
                        {
                            IEdmComplexType elementType = _types[collectionProperty.ElementType] as IEdmComplexType;
                            elementTypeReference = new EdmComplexTypeReference(elementType, false);
                        }
                        else
                        {
                            elementTypeReference = EdmLibHelpers.GetEdmPrimitiveTypeReferenceOrNull(collectionProperty.ElementType);
                        }
                        edmProperty = type.AddStructuralProperty(
                            collectionProperty.PropertyInfo.Name,
                            new EdmCollectionTypeReference(
                                new EdmCollectionType(elementTypeReference),
                                collectionProperty.OptionalProperty));
                        break;

                    default:
                        break;
                }

                if (edmProperty != null && property.PropertyInfo != null)
                {
                    _properties[property.PropertyInfo] = edmProperty;
                }
            }
        }

        private void CreateComplexTypeBody(EdmComplexType type, ComplexTypeConfiguration config)
        {
            CreateStructuralTypeBody(type, config);
        }

        private void CreateEntityTypeBody(EdmEntityType type, EntityTypeConfiguration config)
        {
            CreateStructuralTypeBody(type, config);
            IEdmStructuralProperty[] keys = config.Keys.Select(p => type.DeclaredProperties.OfType<IEdmStructuralProperty>().First(dp => dp.Name == p.PropertyInfo.Name)).ToArray();
            type.AddKeys(keys);
        }

        private void CreatePrimitiveProperty(PrimitivePropertyConfiguration primitiveProperty, EdmStructuredType type, StructuralTypeConfiguration config, out IEdmProperty edmProperty)
        {
            EdmPrimitiveTypeKind typeKind = GetTypeKind(primitiveProperty.PropertyInfo.PropertyType);
            IEdmTypeReference primitiveTypeReference = EdmCoreModel.Instance.GetPrimitive(
                typeKind,
                primitiveProperty.OptionalProperty);

            // Set concurrency token if is entity type, and concurrency token is true
            EdmConcurrencyMode concurrencyMode = EdmConcurrencyMode.None;
            if (config.Kind == EdmTypeKind.Entity && primitiveProperty.ConcurrencyToken)
            {
                concurrencyMode = EdmConcurrencyMode.Fixed;
            }

            var primitiveProp = new EdmStructuralProperty(
                type,
                primitiveProperty.PropertyInfo.Name,
                primitiveTypeReference,
                defaultValueString: null,
                concurrencyMode: concurrencyMode);

            type.AddProperty(primitiveProp);
            edmProperty = primitiveProp;

            // Set Annotation StoreGeneratedPattern
            if (config.Kind == EdmTypeKind.Entity
                && primitiveProperty.StoreGeneratedPattern != DatabaseGeneratedOption.None)
            {
                _directValueAnnotations.Add(
                    new StoreGeneratedPatternAnnotation(primitiveProp, primitiveProperty.StoreGeneratedPattern));
            }
        }

        private void CreateNavigationProperty(EntityTypeConfiguration config)
        {
            Contract.Assert(config != null);

            EdmEntityType entityType = (EdmEntityType)(_types[config.ClrType]);

            foreach (NavigationPropertyConfiguration navProp in config.NavigationProperties)
            {
                EdmNavigationPropertyInfo info = new EdmNavigationPropertyInfo();
                info.Name = navProp.Name;
                info.TargetMultiplicity = navProp.Multiplicity;
                info.Target = _types[navProp.RelatedClrType] as IEdmEntityType;
                info.OnDelete = navProp.OnDeleteAction;

                if (navProp.ReferentialConstraint.Any())
                {
                    info.DependentProperties = ReorderDependentProperties(navProp);
                }

                //TODO: If target end has a multiplity of 1 this assumes the source end is 0..1.
                //      I think a better default multiplicity is *
                entityType.AddUnidirectionalNavigation(info);
            }
        }

        /// <summary>
        /// Builds <see cref="IEdmType"/>'s from <paramref name="configurations"/>
        /// </summary>
        /// <param name="configurations">A collection of <see cref="StructuralTypeConfiguration"/>'s</param>
        /// <returns>The built dictionary of <see cref="StructuralTypeConfiguration"/>'s indexed by their backing CLR type</returns>
        public static Dictionary<Type, IEdmStructuredType> GetTypes(IEnumerable<StructuralTypeConfiguration> configurations)
        {
            if (configurations == null)
            {
                throw Error.ArgumentNull("configurations");
            }

            EdmTypeBuilder builder = new EdmTypeBuilder(configurations);
            return builder.GetEdmTypes();
        }

        /// <summary>
        /// Builds <see cref="IEdmType"/> and <see cref="IEdmProperty"/>'s from <paramref name="configurations"/>
        /// </summary>
        /// <param name="configurations">A collection of <see cref="StructuralTypeConfiguration"/>'s</param>
        /// <returns>The built dictionary of <see cref="IEdmStructuredType"/>'s indexed by their backing CLR type</returns>
        public static EdmTypeMap GetTypesAndProperties(IEnumerable<StructuralTypeConfiguration> configurations)
        {
            if (configurations == null)
            {
                throw Error.ArgumentNull("configurations");
            }

            EdmTypeBuilder builder = new EdmTypeBuilder(configurations);
            return new EdmTypeMap(builder.GetEdmTypes(), builder._directValueAnnotations);
        }

        /// <summary>
        /// Gets the <see cref="EdmPrimitiveTypeKind"/> that maps to the <see cref="Type"/>
        /// </summary>
        /// <param name="clrType">The clr type</param>
        /// <returns>The corresponding Edm primitive kind.</returns>
        public static EdmPrimitiveTypeKind GetTypeKind(Type clrType)
        {
            IEdmPrimitiveType primitiveType = EdmLibHelpers.GetEdmPrimitiveTypeOrNull(clrType);
            if (primitiveType == null)
            {
                throw Error.Argument("clrType", SRResources.MustBePrimitiveType, clrType.FullName);
            }

            return primitiveType.PrimitiveKind;
        }

        private IEnumerable<IEdmStructuralProperty> ReorderDependentProperties(
            NavigationPropertyConfiguration navProperty)
        {
            EntityTypeConfiguration principalEntity = _configurations.OfType<EntityTypeConfiguration>()
                .FirstOrDefault(e => e.ClrType == navProperty.RelatedClrType);
            Contract.Assert(principalEntity != null);

            int keyCount = principalEntity.Keys().Count();
            if (keyCount != navProperty.PrincipalProperties.Count())
            {
                throw Error.InvalidOperation(SRResources.DependentPropertiesNotMatchWithPrincipalKeys,
                    String.Join(",", navProperty.DependentProperties.Select(e => e.PropertyType.FullName)),
                    String.Join(",", principalEntity.Keys.Select(e => e.PropertyInfo.PropertyType.FullName)));
            }

            // OData V3 spec says:
            // The property references for the principal entity MUST be the same property references specified
            // in the edm:Key of the principal entity type.
            IList<IEdmStructuralProperty> dependentProperties = new List<IEdmStructuralProperty>();
            foreach (PropertyConfiguration key in principalEntity.Keys())
            {
                PropertyInfo dependent =
                    navProperty.ReferentialConstraint.FirstOrDefault(r => r.Value.PropertyType == key.PropertyInfo.PropertyType &&
                        r.Value.Name == key.PropertyInfo.Name).Key;

                if (dependent != null)
                {
                    dependentProperties.Add(_properties[dependent] as IEdmStructuralProperty);
                }
            }

            if (keyCount != dependentProperties.Count)
            {
                throw Error.InvalidOperation(SRResources.DependentPropertiesNotMatchWithPrincipalKeys,
                    String.Join(",", navProperty.DependentProperties.Select(e => e.PropertyType.FullName)),
                    String.Join(",", principalEntity.Keys.Select(e => e.PropertyInfo.PropertyType.FullName)));
            }

            return dependentProperties;
        }
    }
}
