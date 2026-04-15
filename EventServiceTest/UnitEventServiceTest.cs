using CSCourse.Controllers;
using CSCourse.Models;
using CSCourse.Services;
using CSCourse.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace EventServiceTest
{
    public class UnitEventServiceTest
    {
        private IEventService _eventMemoryService;
        private EventsController _controller;


        public UnitEventServiceTest()
        {
            _eventMemoryService = new EventMemoryService();
            _controller = new EventsController(_eventMemoryService);
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
            var dto = new EventDto
            {
                Title = "Тест",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1)
            };

            var validator = new DateTimeValidator { ErrorMessage = "EndAt must be later than StartAt." };
            var validationContext = new ValidationContext(dto);

            var result = validator.GetValidationResult(dto.EndAt, validationContext);

            Assert.NotNull(result);
            Assert.Equal("EndAt must be later than StartAt.", result.ErrorMessage);
        }

        [Fact]
        public void DateTimeValidator_StartBeforeEnd_ReturnsSuccess()
        {
            var dto = new EventDto
            {
                Title = "Тест",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var validator = new DateTimeValidator { ErrorMessage = "EndAt must be later than StartAt." };
            var validationContext = new ValidationContext(dto);

            var result = validator.GetValidationResult(dto.EndAt, validationContext);

            Assert.Null(result);
        }

        [Fact]
        public void GetAll_WithValidData_ReturnsOkResultWithPaginatedEvents()
        {
            var existingEvents = _eventMemoryService.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _eventMemoryService.DeleteEvent(@event.Id);
            }

            var testEvents = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Title = "Конференция разработчиков",
                    Description = "Ежегодная конференция...",
                    StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
                },
                new Event
                {
                    Id = 2,
                    Title = "Митап по C#",
                    Description = "Обсуждение новых возможностей языка",
                    StartAt = new DateTime(2026, 12, 5, 14, 0, 0),
                    EndAt = new DateTime(2026, 12, 5, 17, 0, 0)
                }
            };

            foreach (var @event in testEvents)
            {
                _eventMemoryService.CreateEvent(@event);
            }


            var actionResult = _controller.GetAll(null, null, null).Result as OkObjectResult;
            var actualResult = actionResult?.Value as PaginatedResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
            Assert.NotNull(actualResult);
            Assert.Equal(testEvents.Count, actualResult.CountEvents);
            Assert.Equal(testEvents.Count, actualResult.Events.Count);

            for (int i = 0; i < testEvents.Count; i++)
            {
                Assert.Equal(testEvents[i].Id, actualResult.Events[i].Id);
                Assert.Equal(testEvents[i].Title, actualResult.Events[i].Title);
                Assert.Equal(testEvents[i].StartAt, actualResult.Events[i].StartAt);
                Assert.Equal(testEvents[i].EndAt, actualResult.Events[i].EndAt);
            }
        }

        [Fact]
        public void GetAll_WithFilter_ReturnsFilteredResults()
        {
            var existingEvents = _eventMemoryService.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _eventMemoryService.DeleteEvent(@event.Id);
            }

            var allEvents = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Title = "Конференция разработчиков",
                    Description = "Ежегодная конференция...",
                    StartAt = DateTime.Now,
                    EndAt = DateTime.Now.AddHours(8)
                },
                new Event
                {
                    Id = 2,
                    Title = "Встреча команды",
                    Description = "Планерка",
                    StartAt = DateTime.Now.AddDays(1),
                    EndAt = DateTime.Now.AddDays(1).AddHours(2)
                }
            };

            foreach (var @event in allEvents)
            {
                _eventMemoryService.CreateEvent(@event);
            }

            var filterDto = new FilterEventDto
            {
                Title = "конференция"
            };


            var actionResult = _controller.GetAll(filterDto, 1, 10).Result as OkObjectResult;
            var actualResult = actionResult?.Value as PaginatedResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
            Assert.NotNull(actualResult);
            Assert.Single(actualResult.Events);
            Assert.Equal("Конференция разработчиков", actualResult.Events[0].Title);
        }
    }
}
