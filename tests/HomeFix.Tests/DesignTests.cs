using System.Reflection;
using System.Runtime.CompilerServices;
using HomeFix.Core.Contracts;
using HomeFix.Core.Devices;
using HomeFix.Core.Rules;

namespace HomeFix.Tests
{
    /// <summary>
    /// Checks the shape of the library by reflection: the abstraction, the inheritance and the
    /// encapsulation the assignment asks for stay true even if someone edits the code later.
    /// </summary>
    public class DesignTests
    {
        private static readonly Assembly Library = typeof(HomeDevice).Assembly;

        [Fact]
        public void HomeDevice_is_an_abstract_class_that_implements_the_interface()
        {
            Assert.True(typeof(IRepairable).IsInterface);
            Assert.True(typeof(HomeDevice).IsAbstract);
            Assert.True(typeof(IRepairable).IsAssignableFrom(typeof(HomeDevice)));
            Assert.True(typeof(HomeDevice).GetMethod(nameof(HomeDevice.ComputeRepairCost))!.IsAbstract);
            Assert.True(typeof(HomeDevice).GetProperty(nameof(HomeDevice.Kind))!.GetMethod!.IsAbstract);
        }

        [Fact]
        public void Every_kind_of_device_writes_its_own_price_and_description()
        {
            Type[] kinds = WrittenTypes().Where(type => type.IsSubclassOf(typeof(HomeDevice))).ToArray();

            Assert.Equal(4, kinds.Length);
            Assert.All(kinds, kind =>
            {
                Assert.True(kind.IsSealed);
                Assert.Equal(kind, kind.GetMethod(nameof(HomeDevice.ComputeRepairCost))!.DeclaringType);
                Assert.Equal(kind, kind.GetMethod(nameof(HomeDevice.Describe))!.DeclaringType);
            });
        }

        [Fact]
        public void Every_field_of_the_library_is_private()
        {
            string[] nonPrivate = WrittenTypes()
                .Where(type => !type.IsEnum)
                .SelectMany(type => type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .Where(field => !field.IsPrivate)
                .Select(field => $"{field.Name} of {field.DeclaringType!.Name}")
                .ToArray();

            Assert.Empty(nonPrivate);
        }

        [Fact]
        public void No_class_lets_outside_code_set_a_property()
        {
            string[] writable = WrittenTypes()
                .Where(type => type != typeof(DeviceIntake))
                .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => $"{property.Name} of {property.DeclaringType!.Name}")
                .ToArray();

            Assert.Empty(writable);
        }

        [Fact]
        public void The_intake_card_is_written_only_while_it_is_created()
        {
            PropertyInfo[] properties = typeof(DeviceIntake).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.Equal(8, properties.Length);
            Assert.All(properties, property =>
                Assert.Contains(typeof(IsExternalInit), property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers()));
        }

        [Fact]
        public void The_rule_classes_are_static()
        {
            foreach (Type type in new[] { typeof(DataRules), typeof(Tariff) })
            {
                Assert.True(type.IsAbstract && type.IsSealed, $"{type.Name} should be a static class.");
            }
        }

        [Fact]
        public void The_shared_charges_are_private_constants()
        {
            // A string constant is a literal field; a decimal constant is compiled to a read-only
            // static field marked with DecimalConstantAttribute.
            FieldInfo[] fields = typeof(Tariff).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.Equal(3, fields.Length);
            Assert.All(fields, field =>
            {
                Assert.True(field.IsPrivate);
                Assert.True(field.IsLiteral || (field.IsInitOnly && field.IsDefined(typeof(DecimalConstantAttribute))), $"{field.Name} should be a constant.");
            });
        }

        // The types written in the library, without the helpers the compiler adds by itself.
        private static IEnumerable<Type> WrittenTypes()
        {
            return Library.GetTypes().Where(type =>
                type.Namespace?.StartsWith("HomeFix.Core", StringComparison.Ordinal) == true && !IsGenerated(type));
        }

        private static bool IsGenerated(Type type)
        {
            return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
                || (type.DeclaringType is Type outer && IsGenerated(outer));
        }
    }
}
