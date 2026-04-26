using CSCourse.Controllers;
using CSCourse.Models;
using CSCourse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace EventServiceTest
{
    public class UnitEventServiceTest
    {
        [Fact]
        public void EventService_CreateEvent_Success()
        {
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("https://localhost/mocked-url");

            controller.Url = urlHelperMock.Object;

            var validDto = new EventDto
            {
                Title = "Тестовая конференция",
                Description = "Описание мероприятия",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2)
            };

            var result = controller.Post(validDto).Result as CreatedResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
        }

        

        [Fact]
        public void GetAll_WithValidData_ReturnsOkResultWithPaginatedEvents()
        {
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var testEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Конференция разработчиков",
                    Description = "Ежегодная конференция...",
                    StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Митап по C#",
                    Description = "Обсуждение новых возможностей языка",
                    StartAt = new DateTime(2026, 12, 5, 14, 0, 0),
                    EndAt = new DateTime(2026, 12, 5, 17, 0, 0)
                }
            };

            foreach (var @event in testEvents)
            {
                eventService.CreateEvent(@event);
            }


            var actionResult = controller.GetAll(null, null, null).Result as OkObjectResult;
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
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var allEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "КоНфЕрЕнЦиЯ разработчиков",
                    Description = "Ежегодная конференция...",
                    StartAt = DateTime.Now,
                    EndAt = DateTime.Now.AddHours(8)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Встреча команды",
                    Description = "Планерка",
                    StartAt = DateTime.Now.AddDays(1),
                    EndAt = DateTime.Now.AddDays(1).AddHours(2)
                }
            };

            foreach (var @event in allEvents)
            {
                eventService.CreateEvent(@event);
            }

            var filterDto = new FilterEventDto
            {
                Title = "кОнФеРеНцИЯ"
            };


            var actionResult = controller.GetAll(filterDto, 1, 10).Result as OkObjectResult;
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
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var allEvents = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Конференция утром",
                    Description = "Утренняя конференция",
                    StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 12, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Встреча днём",
                    Description = "Дневная встреча",
                    StartAt = new DateTime(2026, 12, 1, 14, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 16, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Вечернее собрание",
                    Description = "Собрание вечером",
                    StartAt = new DateTime(2026, 12, 1, 18, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 20, 0, 0)
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Ранняя планерка",
                    Description = "Утренняя планерка",
                    StartAt = new DateTime(2026, 12, 1, 8, 0, 0),
                    EndAt = new DateTime(2026, 12, 1, 9, 0, 0)
                }
            };

            Guid[] expectedIds = new Guid[2];
            Guid[] notExpectedIds = new Guid[3];
            expectedIds[0] = eventService.CreateEvent(allEvents[0]);
            expectedIds[1] = eventService.CreateEvent(allEvents[3]);

            notExpectedIds[0] = eventService.CreateEvent(allEvents[1]);
            notExpectedIds[1] = eventService.CreateEvent(allEvents[2]);

            var filterDto = new FilterEventDto
            {
                StartAt = new DateTime(2026, 12, 1, 8, 0, 0),  
                EndAt = new DateTime(2026, 12, 1, 12, 0, 0)
            };

            var actionResult = controller.GetAll(filterDto, 1, 10).Result as OkObjectResult;
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
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var testEvent = new Event
            {
                Id = Guid.Empty,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            Guid createdId = eventService.CreateEvent(testEvent);

            var actionResult = controller.GetById(createdId).Result as OkObjectResult;
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
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            Guid nonExistsGuid = Guid.NewGuid();
            var actionResult = controller.GetById(nonExistsGuid).Result as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {nonExistsGuid} not found", actionResult.Value.ToString());
        }


        [Fact]
        public void Put_UpdateExistingEvent_ReturnsNoContent()
        {
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var originalEvent = new Event
            {
                Id = Guid.Empty,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            Guid id = eventService.CreateEvent(originalEvent);

            var updateDto = new EventDto
            {
                Title = "Обновлённая конференция",
                Description = "Описание после обновления",
                StartAt = new DateTime(2026, 12, 2, 9, 0, 0),
                EndAt = new DateTime(2026, 12, 2, 17, 0, 0)
            };

            var actionResult = controller.Put(id, updateDto) as NoContentResult;

            Assert.NotNull(actionResult);
            Assert.Equal(204, actionResult.StatusCode);

            var updatedEvent = eventService.GetEventById(id);
            Assert.Equal(updateDto.Title, updatedEvent?.Title);
            Assert.Equal(updateDto.Description, updatedEvent?.Description);
            Assert.Equal(updateDto.StartAt, updatedEvent?.StartAt);
            Assert.Equal(updateDto.EndAt, updatedEvent?.EndAt);
        }

        [Fact]
        public void Put_UpdateNonExistingEvent_ReturnsNotFound()
        {
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var updateDto = new EventDto
            {
                Title = "Попытка обновления",
                Description = "Это событие не существует",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(2)
            };

            Guid nonExistsGuid = Guid.NewGuid();

            var actionResult = controller.Put(nonExistsGuid, updateDto) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {nonExistsGuid} not found", actionResult.Value.ToString());
        }

        [Fact]
        public void Delete_DeleteExistingEvent_ReturnsOk()
        {
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            var testEvent = new Event
            {
                Id = Guid.Empty,
                Title = "Конференция разработчиков",
                Description = "Ежегодная конференция...",
                StartAt = new DateTime(2026, 12, 1, 10, 0, 0),
                EndAt = new DateTime(2026, 12, 1, 18, 0, 0)
            };

            Guid createdId = eventService.CreateEvent(testEvent);

            var actionResult = controller.Delete(createdId) as OkResult;

            Assert.NotNull(actionResult);
            Assert.Equal(200, actionResult.StatusCode);

            var allEvents = eventService.GetAll(1, int.MaxValue).Events;
            Assert.DoesNotContain(testEvent, allEvents);
            Console.WriteLine(allEvents);
            Assert.Empty(allEvents);
        }

        [Fact]
        public void Delete_DeleteNonExistingEvent_ReturnsNotFound()
        {
            var eventService = new EventMemoryService();
            var bookingService = new BookingMemoryService();
            var controller = new EventsController(eventService, bookingService);

            Guid nonExistsGuid = Guid.NewGuid();

            var actionResult = controller.Delete(nonExistsGuid) as NotFoundObjectResult;

            Assert.NotNull(actionResult);
            Assert.Equal(404, actionResult.StatusCode);

            Assert.NotNull(actionResult.Value);
            Assert.Contains($"Event with index {nonExistsGuid} not found", actionResult.Value.ToString());

            var remainingEvents = eventService.GetAll(1, int.MaxValue).Events;
            Assert.All(remainingEvents, e => Assert.NotEqual(nonExistsGuid, e.Id));
        }
    }
}
