using CSCourse.Controllers;
using CSCourse.Models;
using CSCourse.Services;
using CSCourse.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Xml;

namespace EventServiceTest
{
    public class EventServiceFixture : IDisposable
    {
        public IEventService Service { get; private set; }
        public EventsController Controller { get; private set; }

        public EventServiceFixture()
        {
            Service = new EventMemoryService();
            Controller = new EventsController(Service);
        }

        public void Dispose()
        {
            var allEvents = Service.GetAll(1, int.MaxValue).Events;
            foreach (var e in allEvents) Service.DeleteEvent(e.Id);
        }
    }

    public class UnitEventServiceTest : IClassFixture<EventServiceFixture>
    {
        private readonly IEventService _service;
        private readonly EventsController _controller;

        public UnitEventServiceTest(EventServiceFixture fixture)
        {
            _service = fixture.Service;
            _controller = fixture.Controller;
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
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _service.DeleteEvent(@event.Id);
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
                _service.CreateEvent(@event);
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
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _service.DeleteEvent(@event.Id);
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
                _service.CreateEvent(@event);
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

        [Fact]
        public void GetAll_WithDateFilter_ReturnsFilteredByDateResults()
        {
            var allEvents = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Title = "Конференция утром",
                    Description = "Утренняя конференция",
                    StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 12, 0, 0)
                },
                new Event
                {
                    Id = 2,
                    Title = "Встреча днём",
                    Description = "Дневная встреча",
                    StartAt = new DateTime(2026, 12, 1, 14, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 16, 0, 0)
                },
                new Event
                {
                    Id = 3,
                    Title = "Вечернее собрание",
                    Description = "Собрание вечером",
                    StartAt = new DateTime(2026, 12, 1, 18, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 20, 0, 0)
                },
                new Event
                {
                    Id = 4,
                    Title = "Ранняя планерка",
                    Description = "Утренняя планерка",
                    StartAt = new DateTime(2026, 12, 1, 8, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 9, 0, 0)
                }
            };

            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _service.DeleteEvent(@event.Id);
            }
            int expectedId = _service.CreateEvent(allEvents[0]);
            foreach (var @event in allEvents[1..])
            {
                _service.CreateEvent(@event);
            }

            var filterDto = new FilterEventDto
            {
                StartAt = new DateTime(2026, 12, 1, 11, 0, 0),  
                EndAt = new DateTime(2026, 12, 1, 12, 0, 0)
            };

            var actionResult = _controller.GetAll(filterDto, 1, 10).Result as OkObjectResult;
            var actualResult = actionResult?.Value as PaginatedResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
            Assert.NotNull(actualResult);

            Assert.Equal(allEvents.Count, actualResult.CountEvents);
            Assert.Equal(1, actualResult.Events.Count);

            var returnedIds = actualResult.Events.Select(e => e.Id).ToArray();
            Assert.Contains(expectedId, returnedIds); 

            Assert.DoesNotContain(++expectedId, returnedIds); 
            Assert.DoesNotContain(++expectedId, returnedIds);
            Assert.DoesNotContain(++expectedId, returnedIds); 

            var firstEvent = actualResult.Events[0];
            Assert.Equal("Конференция утром", firstEvent.Title);
            Assert.True(firstEvent.StartAt <= filterDto.StartAt);
            Assert.True(firstEvent.EndAt >= filterDto.EndAt);
        }

        [Fact]
        public void GetById_ExistingEvent_ReturnsOkResultWithEvent()
        {
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _service.DeleteEvent(@event.Id);
            }

            var testEvent = new Event
            {
                Id = 1,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            int createdId = _service.CreateEvent(testEvent);

            var actionResult = _controller.GetById(createdId).Result as OkObjectResult;
            var actualEvent = actionResult?.Value as Event;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
            Assert.NotNull(actualEvent);

            Assert.Equal(createdId, actualEvent.Id);
            Assert.Equal(testEvent.Title, actualEvent.Title);
            Assert.Equal(testEvent.Description, actualEvent.Description);
            Assert.Equal(testEvent.StartAt, actualEvent.StartAt);
            Assert.Equal(testEvent.EndAt, actualEvent.EndAt);
        }

        [Fact]
        public void GetById_NonExistingEvent_ReturnsNotFound()
        {
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                if (@event.Id == 999)
                {
                    _service.DeleteEvent(999);
                }
            }

            var actionResult = _controller.GetById(999).Result as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains("Event with index 999 not found", actionResult.Value.ToString());
        }


        [Fact]
        public void Put_UpdateExistingEvent_ReturnsNoContent()
        {
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _service.DeleteEvent(@event.Id);
            }

            var originalEvent = new Event
            {
                Id = 1,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            _service.CreateEvent(originalEvent);

            var updateDto = new EventDto
            {
                Title = "Обновлённая конференция",
                Description = "Описание после обновления",
                StartAt = new DateTime(2026, 12, 2, 9, 0, 0),
                EndAt = new DateTime(2026, 12, 2, 17, 0, 0)
            };

            var actionResult = _controller.Put(1, updateDto) as NoContentResult;

            Assert.NotNull(actionResult);
            Assert.Equal(204, actionResult.StatusCode);

            var updatedEvent = _service.GetEventById(1);
            Assert.Equal(updateDto.Title, updatedEvent.Title);
            Assert.Equal(updateDto.Description, updatedEvent.Description);
            Assert.Equal(updateDto.StartAt, updatedEvent.StartAt);
            Assert.Equal(updateDto.EndAt, updatedEvent.EndAt);
        }

        [Fact]
        public void Put_UpdateNonExistingEvent_ReturnsNotFound()
        {
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                if (@event.Id == 999)
                {
                    _service.DeleteEvent(999);
                }
            }

            var updateDto = new EventDto
            {
                Title = "Попытка обновления",
                Description = "Это событие не существует",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(2)
            };

            var actionResult = _controller.Put(999, updateDto) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains("Event with index 999 not found", actionResult.Value.ToString());
        }

        [Fact]
        public void Delete_DeleteExistingEvent_ReturnsOk()
        {
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                _service.DeleteEvent(@event.Id);
            }
            var testEvent = new Event
            {
                Id = 1,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            int createdId = _service.CreateEvent(testEvent);

            var actionResult = _controller.Delete(createdId) as OkResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var allEvents = _service.GetAll(1, int.MaxValue).Events;
            Assert.DoesNotContain(testEvent, allEvents);
            Assert.Empty(allEvents);
        }

        [Fact]
        public void Delete_DeleteNonExistingEvent_ReturnsNotFound()
        {
            var existingEvents = _service.GetAll(1, int.MaxValue).Events;
            foreach (var @event in existingEvents)
            {
                if (@event.Id == 999)
                {
                    _service.DeleteEvent(999);
                }
            }

            var actionResult = _controller.Delete(999) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains("Event with index 999 not found", actionResult.Value.ToString());

            var remainingEvents = _service.GetAll(1, int.MaxValue).Events;
            Assert.All(remainingEvents, e => Assert.NotEqual(999, e.Id));
        }
    }
}
