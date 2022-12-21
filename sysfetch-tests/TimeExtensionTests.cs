using sysfetch.Extensions;

namespace sysfetch_tests
{
    public class HumanReadableStringTests
    {
        [Test]
        public void GetHumanReadableString_ZeroSeconds_ReturnsZeroSeconds()
        {
            // Arrange
            int seconds = 0;
            
            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void GetHumanReadableString_OneSecond_ReturnsOneSecond()
        {
            // Arrange
            const int seconds = 1;

            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo("1 second"));
        }

        [Test]
        public void GetHumanReadableString_OneMinute_ReturnsOneMinute()
        {
            // Arrange
            int seconds = 60;

            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo("1 minute"));
        }

        [Test]
        public void GetHumanReadableString_OneHour_ReturnsOneHour()
        {
            // Arrange
            int seconds = 3600;

            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo("1 hour"));
        }

        [Test]
        public void GetHumanReadableString_OneDay_ReturnsOneDay()
        {
            // Arrange
            int seconds = 86400;

            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo("1 day"));
        }

        [Test]
        public void GetHumanReadableString_TwoDays_ReturnsTwoDays()
        {
            // Arrange
            int seconds = 86400 * 2;

            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo("2 days"));
        }

        [Test]
        public void GetHumanReadableString_AllUnits_ReturnsCorrectString()
        {
            // Arrange
            int seconds = 86400 * 2 + 3600 * 3 + 60 * 4 + 5;

            // Act
            string result = seconds.GetHumanReadableString();

            // Assert
            Assert.That(result, Is.EqualTo("2 days 3 hours 4 minutes 5 seconds"));
        }
    }
}
