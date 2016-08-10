
using System.Globalization;
using Xunit;

namespace StrangerData.UnitTests
{
    public class AnyTests
    {
        [Fact]
        public void Decimal1x2Test()
        {
            decimal value = Any.Decimal(1,2);
            string[] v = value.ToString().Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
            Assert.True(v[0].Length <= 2, "Erro na quantidade de digitos.");
            Assert.True(v[1].Length <= 5, "Erro na quantidade de decimais.");
        }

        [Fact]
        public void Decimal5x4Test()
        {
            decimal value = Any.Decimal(5, 4);
            string[] v = value.ToString().Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
            Assert.True(v[0].Length <= 5, "Erro na quantidade de digitos.");
            Assert.True(v[1].Length <= 4, "Erro na quantidade de decimais.");
        }

        [Fact]
        public void Decimal10x8Test()
        {
            decimal value = Any.Decimal(10, 8);
            string[] v = value.ToString().Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
            Assert.True(v[0].Length <= 10, "Erro na quantidade de digitos.");
            Assert.True(v[1].Length <= 8, "Erro na quantidade de decimais.");
        }

        [Fact]
        public void Decimal4x0Test()
        {
            decimal value = Any.Decimal(4, 0);
            string[] v = value.ToString().Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
            Assert.True(v.Length == 1, "Erro, foi gerado valores em decimal");
            Assert.True(v[0].Length <= 4, "Erro na quantidade de digitos.");
        }
    }
}
