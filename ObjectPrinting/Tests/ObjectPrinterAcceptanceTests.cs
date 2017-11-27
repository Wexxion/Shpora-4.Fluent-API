using System;
using System.Globalization;
using FluentAssertions;
using NUnit.Framework;

namespace ObjectPrinting.Tests
{
    [TestFixture]
    public class ObjectPrinterAcceptanceTests
    {
        [Test]
        public void Demo()
        {
            var testPerson = new Person
            {
                Name = "Alexander", Age = 19, Height = 200, Weight = 120.1231f,
                Father = new Person
                {
                    Name = "Danny", Age = 42, Height = 181.9d, Weight = 75.123f,
                    Mother = new Person { Name = "Anna", Age = 96, Height = 156}
                }
            };

            var printer = ObjectPrinter.For<Person>()
                //1. Исключить из сериализации свойства определенного типа
                .Excluding<Guid>()
                //2. Указать альтернативный способ сериализации для определенного типа
                .Printing<int>().Using(i => i.ToString("X"))
                //3. Для числовых типов указать культуру
                .Printing<double>().Using(CultureInfo.CreateSpecificCulture("de-DE")) // Выводит с запятой
                .Printing<float>().Using(CultureInfo.InvariantCulture) // Выводит с точкой
                //4. Настроить сериализацию конкретного свойства
                .Printing(p => p.Age).Using(age => $"{age} years old")
                //5. Настроить обрезание строковых свойств (метод должен быть виден только для строковых свойств)
                .Printing(p => p.Name).TrimmedToLength(4);
                //6. Исключить из сериализации конкретного свойства
                

            var s1 = printer.PrintToString(testPerson);

            //7. Синтаксический сахар в виде метода расширения, сериализующего по-умолчанию
            testPerson.PrintToString();

            //8. ...с конфигурированием
            testPerson.PrintToString(s => s.Excluding(p => p.Age));
            Console.WriteLine(s1);
        }

        private Person person;
        [SetUp]
        public void SetUp()
        {
            person = new Person { Name = "Alexander", Age = 19, Height = 200, Weight = 120.123f };
        }

        [Test]
        public void TestPropertyExcluding()
        {
            var printer = ObjectPrinter.For<Person>()
                .Excluding(p => p.Id);
            var result = printer.PrintToString(person);
            result.Contains("Id").Should().BeFalse();
            person.PrintToString().Contains("Id").Should().BeTrue();
        }

        [Test]
        public void TestTypeExcluding()
        {
            var printer = ObjectPrinter.For<Person>()
                .Excluding<int>();
            var result = printer.PrintToString(person);
            result.Contains("Age").Should().BeFalse();
            person.PrintToString().Contains("Age").Should().BeTrue();
        }

        [Test]
        public void TestCustomTypeSerialization()
        {
            var printer = ObjectPrinter.For<Person>()
                .Printing<int>().Using(i => $"int({i})");
            var result = printer.PrintToString(person);
            result.Contains($"int({person.Age})").Should().BeTrue();
            person.PrintToString().Contains($"int({person.Age})").Should().BeFalse();
        }

        [Test]
        public void TestCustomPropertySerialization()
        {
            var printer = ObjectPrinter.For<Person>()
                .Printing(p => p.Age).Using(i => $"{i} years old");
            var result = printer.PrintToString(person);
            result.Contains($"{person.Age} years old").Should().BeTrue();
            person.PrintToString().Contains($"{person.Age} years old").Should().BeFalse();
        }

        [TestCase("de-DE", "120,123")]
        [TestCase("en-EN", "120.123")]
        public void TestNumericCultureSerialization(string culture, string expectedNumericString)
        {
            var printer = ObjectPrinter.For<Person>()
                .Printing<float>().Using(CultureInfo.CreateSpecificCulture(culture));
            var result = printer.PrintToString(person);

            result.Contains(expectedNumericString).Should().BeTrue();
        }

        [TestCase(4, "Alex")]
        [TestCase(6, "Alexan")]
        public void TestStringTrimming(int count, string valueAfterTrimming)
        {
            var printer = ObjectPrinter.For<Person>()
                .Printing(p => p.Name).TrimmedToLength(count);
            var result = printer.PrintToString(person);

            result.Contains(valueAfterTrimming).Should().BeTrue();
            result.Contains(person.Name).Should().BeFalse();
            person.PrintToString().Contains(person.Name).Should().BeTrue();
        }
    }
}