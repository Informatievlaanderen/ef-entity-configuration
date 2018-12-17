namespace Be.Vlaanderen.Basisregisters.EntityFrameworkCore.EntityTypeConfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.EntityFrameworkCore;

    public static class ModelBuilderExtenions
    {
        private static IEnumerable<Type> GetMappingTypes(this Assembly assembly, Type mappingInterface)
        {
            return assembly
                .GetTypes()
                .Where(x =>
                    !x.GetTypeInfo().IsAbstract &&
                    x.GetInterfaces().Any(y =>
                        y.GetTypeInfo().IsGenericType && y.GetGenericTypeDefinition() == mappingInterface));
        }

        public static void AddEntityConfigurationsFromAssembly(this ModelBuilder modelBuilder, Assembly assembly)
        {
            var mappingTypes = assembly.GetMappingTypes(typeof(IEntityTypeConfiguration<>));

            foreach (var config in mappingTypes.Select(Activator.CreateInstance))
            {
                // public class EntityConfiguration : IEntityTypeConfiguration<Entity>
                var entityTypeConfiguration = config.GetType().GetInterface("IEntityTypeConfiguration`1");
                var entity = entityTypeConfiguration.GetGenericArguments()[0]; // contains entity type
                var applyConfigurationMethod = (from method in typeof(ModelBuilder).GetMethods()
                                               where method.Name == "ApplyConfiguration"
                                               where method.GetParameters().Select(p => p.ParameterType.GetGenericTypeDefinition()).SequenceEqual(new [] { typeof(IEntityTypeConfiguration<>) })
                                               select method).Single();

                var applyConfiguration = applyConfigurationMethod.MakeGenericMethod(entity);

                // Console.WriteLine($"Applying configuration {config.GetType().FullName}");
                applyConfiguration.Invoke(modelBuilder, new[] { config });
            }
        }
    }
}
