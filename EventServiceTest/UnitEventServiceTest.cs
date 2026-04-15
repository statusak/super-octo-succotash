using CSCourse.Controllers;
using CSCourse.Models;
using CSCourse.Services;
using CSCourse.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace EventServiceTest
{
    public class UnitEventServiceTest
    {
        private Mock<IEventService> _mockEventService;
        private EventsController _controller;


        public UnitEventServiceTest()
        {
            _mockEventService = new Mock<IEventService>();
            _controller = new EventsController(_mockEventService.Object);

            _mockEventService.Setup(s => s.CreateEvent(It.IsAny<Event>()));
        }


        [Fact]
        public void EventService_CreateEvent_Success()
        {
            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            

            var result = _controller.Post(validDto) as CreatedResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
        }

        [Fact]
        public void DateTimeValidator_EndBeforeStart_ReturnsError()
        {
            // Arrange
            var dto = new EventDto
            {
                Title = "Тест",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1)
            };

            var validator = new DateTimeValidator { ErrorMessage = "EndAt must be later than StartAt." };
            var validationContext = new ValidationContext(dto);

            // Act
            var result = validator.GetValidationResult(dto.EndAt, validationContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EndAt must be later than StartAt.", result.ErrorMessage);
        }

        [Fact]
        public void DateTimeValidator_StartBeforeEnd_ReturnsSuccess()
        {
            // Arrange
            var dto = new EventDto
            {
                Title = "Тест",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var validator = new DateTimeValidator { ErrorMessage = "EndAt must be later than StartAt." };
            var validationContext = new ValidationContext(dto);

            // Act
            var result = validator.GetValidationResult(dto.EndAt, validationContext);

            // Assert
            Assert.Null(result);
        }
    }
}
