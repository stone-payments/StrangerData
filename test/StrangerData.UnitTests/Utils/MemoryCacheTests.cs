using FluentAssertions;
using Moq;
using StrangerData.Utils;
using System;
using Xunit;

namespace StrangerData.UnitTests.Utils
{
    public class MemoryCacheTests
    {
        [Fact]
        public void TryGetFromCache_BothKeysNotExists_InvokeFuncAndReturnFuncValue()
        {
            // Arrange
            string expected = Any.String();
            
            Mock<Func<object>> funcMock = new Mock<Func<object>>();
            funcMock.Setup(f => f()).Returns(expected);

            string nonExistentPrimaryKey = Any.String();
            string nonExistentSecondaryKey = Any.String();

            // Act
            string actual = MemoryCache.TryGetFromCache<string>(nonExistentPrimaryKey, nonExistentSecondaryKey, funcMock.Object);

            // Assert
            actual.Should().Be(expected);
            funcMock.Verify(f => f(), Times.Once);
        }

        [Fact]
        public void TryGetFromCache_SecondaryKeyNotExists_InvokeFuncAndReturnFuncValue()
        {
            // Arrange
            string expected = Any.String();

            Mock<Func<object>> funcMock = new Mock<Func<object>>();
            funcMock.Setup(f => f()).Returns(expected);

            string primaryKey = Any.String();
            MemoryCache.TryGetFromCache<string>(primaryKey, Any.String(), () => Any.String());

            string nonExistentSecondaryKey = Any.String();

            // Act
            string actual = MemoryCache.TryGetFromCache<string>(primaryKey, nonExistentSecondaryKey, funcMock.Object);

            // Assert
            actual.Should().Be(expected);
            funcMock.Verify(f => f(), Times.Once);
        }

        [Fact]
        public void TryGetFromCache_BothKeysExists_NotInvokeFuncAndReturnExistentValue()
        {
            // Arrange
            Mock<Func<object>> funcMock = new Mock<Func<object>>();
            funcMock.Setup(f => f()).Returns(Any.String());

            string primaryKey = Any.String();
            string secondaryKey = Any.String();
            string expected = Any.String();
            MemoryCache.TryGetFromCache<string>(primaryKey, secondaryKey, () => expected);

            // Act
            string actual = MemoryCache.TryGetFromCache<string>(primaryKey, secondaryKey, funcMock.Object);

            // Assert
            actual.Should().Be(expected);
            funcMock.Verify(f => f(), Times.Never);
        }
    }
}
