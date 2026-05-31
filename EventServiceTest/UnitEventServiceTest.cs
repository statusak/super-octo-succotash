using CSCourse.Controllers;
using CSCourse.Interfaces;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventServiceTest
{
    public class UnitEventServiceTest
    {
        private readonly EventService _eventService;
        private readonly EventsController _controller;

        public UnitEventServiceTest()
        {
            _eventService = new EventService();
            var bookingService = new BookingService(_eventService);
            var logger = NullLogger<EventsController>.Instance;
            _controller = new EventsController(_eventService, bookingService, logger);
        }

        [Fact]
        public void EventService_CreateEvent_Success()
        {
            var validDto = new EventCreateDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                TotalSeats = 100,
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var result = _controller.Post(validDto).Result as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
        }

        [Fact]
        public void GetAll_WithValidData_ReturnsOkResultWithPaginatedEvents()
        {
            var testEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Конференция разработчиков",
                    Description = "Ежегодная конференция...",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Митап по C#",
                    Description = "Обсуждение новых возможностей языка",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = new DateTime(2026, 12, 5, 14, 0, 0),
                    EndAt = new DateTime(2026, 12, 5, 17, 0, 0)
                }
            };

            foreach (var @event in testEvents)
            {
                _eventService.CreateEvent(@event);
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
            var allEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "КоНфЕрЕнЦиЯ разработчиков",
                    Description = "Ежегодная конференция...",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = DateTime.Now,
                    EndAt = DateTime.Now.AddHours(8)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Встреча команды",
                    Description = "Планерка",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = DateTime.Now.AddDays(1),
                    EndAt = DateTime.Now.AddDays(1).AddHours(2)
                }
            };

            foreach (var @event in allEvents)
            {
                _eventService.CreateEvent(@event);
            }

            var filterDto = new FilterEventDto
            {
                Title = "кОнФеРеНцИЯ"
            };


            var actionResult = _controller.GetAll(filterDto, 1, 10).Result as OkObjectResult;
            var actualResult = actionResult?.Value as PaginatedResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
            Assert.NotNull(actualResult);
            Assert.Single(actualResult.Events);
            Assert.Equal("КоНфЕрЕнЦиЯ разработчиков", actualResult.Events[0].Title);
        }

        [Fact]
        public void GetAll_WithDateFilter_ReturnsFilteredByDateResults()
        {
            var allEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Конференция утром",
                    Description = "Утренняя конференция",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 12, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Встреча днём",
                    Description = "Дневная встреча",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = new DateTime(2026, 12, 1, 14, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 16, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Вечернее собрание",
                    Description = "Собрание вечером",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = new DateTime(2026, 12, 1, 18, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 20, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Ранняя планерка",
                    Description = "Утренняя планерка",
                    TotalSeats = 100,
                    AvailableSeats = 100,
                    StartAt = new DateTime(2026, 12, 1, 8, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 9, 0, 0)
                }
            };

            Guid[] expectedIds = new Guid[2];
            Guid[] notExpectedIds = new Guid[3];
            expectedIds[0] = _eventService.CreateEvent(allEvents[0]);
            expectedIds[1] = _eventService.CreateEvent(allEvents[3]);

            notExpectedIds[0] = _eventService.CreateEvent(allEvents[1]);
            notExpectedIds[1] = _eventService.CreateEvent(allEvents[2]);

            var filterDto = new FilterEventDto
            {
                StartAt = new DateTime(2026, 12, 1, 8, 0, 0),  
                EndAt = new DateTime(2026, 12, 1, 12, 0, 0)
            };

            var actionResult = _controller.GetAll(filterDto, 1, 10).Result as OkObjectResult;
            var actualResult = actionResult?.Value as PaginatedResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);
            Assert.NotNull(actualResult);

            Assert.Equal(allEvents.Count, actualResult.CountEvents);

            var returnedIds = actualResult.Events.Select(e => e.Id).ToArray();
            foreach ( var expectedId in expectedIds)
            {
                Assert.Contains(expectedId, returnedIds);
            }

            foreach (var notExpectedId in notExpectedIds)
            {
                Assert.DoesNotContain(notExpectedId, returnedIds);
            }
        }

        [Fact]
        public void GetById_ExistingEvent_ReturnsOkResultWithEvent()
        {
            var testEvent = new Event
            {
                Id = Guid.Empty,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            Guid createdId = _eventService.CreateEvent(testEvent);

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
            Guid nonExistsGuid = Guid.NewGuid();
            var actionResult = _controller.GetById(nonExistsGuid).Result as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {nonExistsGuid} not found", actionResult.Value.ToString());
        }


        [Fact]
        public void Put_UpdateExistingEvent_ReturnsNoContent()
        {
            var originalEvent = new Event
            {
                Id = Guid.Empty,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            Guid id = _eventService.CreateEvent(originalEvent);

            var updateDto = new EventUpdateDto
            {
                Title = "Обновлённая конференция",
                Description = "Описание после обновления",
                StartAt = new DateTime(2026, 12, 2, 9, 0, 0),
                EndAt = new DateTime(2026, 12, 2, 17, 0, 0)
            };

            var actionResult = _controller.Put(id, updateDto) as NoContentResult;

            Assert.NotNull(actionResult);
            Assert.Equal(204, actionResult.StatusCode);

            var updatedEvent = _eventService.GetEventById(id);
            Assert.Equal(updateDto.Title, updatedEvent?.Title);
            Assert.Equal(updateDto.Description, updatedEvent?.Description);
            Assert.Equal(originalEvent.TotalSeats, updatedEvent?.TotalSeats);
            Assert.Equal(originalEvent.AvailableSeats, updatedEvent?.AvailableSeats);
            Assert.Equal(updateDto.StartAt, updatedEvent?.StartAt);
            Assert.Equal(updateDto.EndAt, updatedEvent?.EndAt);
        }

        [Fact]
        public void Put_UpdateNonExistingEvent_ReturnsNotFound()
        {
            var updateDto = new EventUpdateDto
            {
                Title = "Попытка обновления",
                Description = "Это событие не существует",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(2)
            };

            Guid nonExistsGuid = Guid.NewGuid();

            var actionResult = _controller.Put(nonExistsGuid, updateDto) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {nonExistsGuid} not found", actionResult.Value.ToString());
        }

        [Fact]
        public void Delete_DeleteExistingEvent_ReturnsOk()
        {
            var testEvent = new Event
            {
                Id = Guid.Empty,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                TotalSeats = 100,
                AvailableSeats = 100,
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            Guid createdId = _eventService.CreateEvent(testEvent);

            var actionResult = _controller.Delete(createdId) as OkResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var allEvents = _eventService.GetAll(1, int.MaxValue).Events;
            Assert.DoesNotContain(testEvent, allEvents);
            Assert.Empty(allEvents);
        }

        [Fact]
        public void Delete_DeleteNonExistingEvent_ReturnsNotFound()
        {
            Guid nonExistsGuid = Guid.NewGuid();

            var actionResult = _controller.Delete(nonExistsGuid) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {nonExistsGuid} not found", actionResult.Value.ToString());

            var remainingEvents = _eventService.GetAll(1, int.MaxValue).Events;
            Assert.All(remainingEvents, e => Assert.NotEqual(nonExistsGuid, e.Id));
        }
    }
}
