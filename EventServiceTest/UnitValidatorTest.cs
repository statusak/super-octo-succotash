using CSCourse.Models;
using System.ComponentModel.DataAnnotations;

namespace EventServiceTest
{
    public class UnitValidatorTest
    {
        [Fact]
        public void DateTimeValidator_EndBeforeStart_ReturnsError()
        {
            var invalidDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1)
            };

            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(
                invalidDto,
                new ValidationContext(invalidDto),
                validationResults,
                true
            );

            Assert.False(isValid);
            Assert.NotEmpty(validationResults);
            Assert.Contains(
                validationResults,
                vr => vr.ErrorMessage == "EndAt must be later than StartAt."
            );

        }

        [Fact]
        public void DateTimeValidator_StartBeforeEnd_ReturnsSuccess()
        {
            var invalidDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(
                invalidDto,
                new ValidationContext(invalidDto),
                validationResults,
                true
            );

            Assert.True(isValid);
            Assert.Empty(validationResults);
        }
    }
}
