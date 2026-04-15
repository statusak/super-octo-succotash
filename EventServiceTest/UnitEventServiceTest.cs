using CSCourse.Models;
using CSCourse.Services;

namespace EventServiceTest
{
    public class UnitEventServiceTest
    {
        private readonly IEventService _service;

        public UnitEventServiceTest()
        {
            _service = new EventMemoryService();
        }


        [Fact]
        public void EventService_CreateEvent_Success()
        {
            var eventToCreate = new Event
            {
                Id = 0,
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            int id = _service.CreateEvent(eventToCreate);

            var createdEvent = _service.GetEventById(id);
            Assert.NotNull(createdEvent);
            Assert.Equal(eventToCreate.Id, id);
            Assert.Equal(eventToCreate.Title, createdEvent.Title);
            Assert.Equal(eventToCreate.Description, createdEvent.Description);
            Assert.Equal(eventToCreate.StartAt, createdEvent.StartAt);
            Assert.Equal(eventToCreate.EndAt, createdEvent.EndAt);
        }
    }
}
