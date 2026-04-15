using CSCourse.Controllers;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

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

            _mockEventService.Setup(s => s.CreateEvent(It.IsAny<Event>()));

            var result = _controller.Post(validDto) as CreatedResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
        }
    }
}
