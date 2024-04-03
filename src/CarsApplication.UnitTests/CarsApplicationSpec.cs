using Application.Interfaces;
using CarsApplication.Persistence;
using CarsApplication.Persistence.ReadModels;
using CarsDomain;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared.Cars;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace CarsApplication.UnitTests;

[Trait("Category", "Unit")]
public class CarsApplicationSpec
{
    private readonly CarsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ICarRepository> _repository;

    public CarsApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _repository = new Mock<ICarRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<CarRoot>(), It.IsAny<CancellationToken>()))
            .Returns((CarRoot car, CancellationToken _) => Task.FromResult<Result<CarRoot, Error>>(car));
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId).Returns("acallerid");
        _application = new CarsApplication(_recorder.Object, _idFactory.Object,
            _repository.Object);
    }

    [Fact]
    public async Task WhenDeleteCarAsyncWithUnknownCar_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CarRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.DeleteCarAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDeleteCarAsync_ThenDeletes()
    {
        var car = CarRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        car.Value.SetOwnership(VehicleOwner.Create("acallerid").Value);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));

        var result =
            await _application.DeleteCarAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenGetCarAsyncWithUnknownCar_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CarRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.GetCarAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetCarAsync_ThenReturnsCar()
    {
        var car = CarRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        car.Value.SetManufacturer(Manufacturer.Create(2023, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0])
            .Value);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));

        var result =
            await _application.GetCarAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Manufacturer!.Make.Should().Be(Manufacturer.AllowedMakes[0]);
        result.Value.Manufacturer!.Model.Should().Be(Manufacturer.AllowedModels[0]);
        result.Value.Manufacturer!.Year.Should().Be(2023);
    }

    [Fact]
    public async Task WhenScheduleMaintenanceCarAsyncWithUnknownCar_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CarRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.ScheduleMaintenanceCarAsync(_caller.Object, "anorganizationid", "anid", DateTime.UtcNow,
                DateTime.UtcNow, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenScheduleMaintenanceCarAsync_ThenSchedulesMaintenance()
    {
        var car = SetupRegisteredCar();
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));
        var fromUtc = DateTime.UtcNow.AddDays(2);
        var toUtc = fromUtc.AddHours(1);

        var result =
            await _application.ScheduleMaintenanceCarAsync(_caller.Object, "anorganizationid", "anid", fromUtc,
                toUtc, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<CarRoot>(c =>
            (c.Unavailabilities.Count == 1)
            & (c.Unavailabilities[0].Slot!.From == fromUtc)
            & (c.Unavailabilities[0].Slot!.To == toUtc)
            & (c.Unavailabilities[0].CarId! == "anid")
            & (c.Unavailabilities[0].CausedBy!.Reason == UnavailabilityCausedBy.Maintenance)
            & (c.Unavailabilities[0].CausedBy!.Reference == null)
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRegisterCarAsync_ThenRegistersCar()
    {
        var result =
            await _application.RegisterCarAsync(_caller.Object, "anorganizationid", Manufacturer.AllowedMakes[0],
                Manufacturer.AllowedModels[0], 2023, Jurisdiction.AllowedCountries[0], "aplate",
                CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Manufacturer!.Make.Should().Be(Manufacturer.AllowedMakes[0]);
        result.Value.Manufacturer!.Model.Should().Be(Manufacturer.AllowedModels[0]);
        result.Value.Manufacturer!.Year.Should().Be(2023);
        result.Value.Managers!.First().Id.Should().Be("acallerid");
        result.Value.Owner!.Id.Should().Be("acallerid");
        result.Value.Plate!.Jurisdiction.Should().Be(Jurisdiction.AllowedCountries[0]);
        result.Value.Plate!.Number.Should().Be("aplate");
        result.Value.Status.Should().Be(CarStatus.Registered.ToString());
        _repository.Verify(rep => rep.SaveAsync(It.Is<CarRoot>(c =>
            c.Id == "anid"
            && c.OrganizationId == "anorganizationid"
            && c.Manufacturer.Value.Make == Manufacturer.AllowedMakes[0]
            && c.Manufacturer.Value.Model == Manufacturer.AllowedModels[0]
            && c.Manufacturer.Value.Year == 2023
            && c.Unavailabilities.Count == 0
            && c.Owner.Value.OwnerId == "acallerid"
            && c.Managers.Managers.Count == 1
            && c.Managers.Managers[0] == "acallerid"
            && c.License.Value.Jurisdiction == Jurisdiction.AllowedCountries[0]
            && c.License.Value.Number == "aplate"
            && c.Status == CarStatus.Registered
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenReleaseAvailabilityAsyncWithUnknownCar_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CarRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.ReleaseCarAvailabilityAsync(_caller.Object, "anorganizationid", "anid", DateTime.UtcNow,
                DateTime.UtcNow, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenReleaseCarAvailabilityAsyncAndHasUnavailability_ThenUnavailabilityRemoved()
    {
        var car = CarRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        car.Value.SetManufacturer(Manufacturer.Create(2023, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0])
            .Value);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));
        var fromUtc = DateTime.UtcNow.SubtractHours(1);
        var toUtc = fromUtc.AddHours(1);
        car.Value.TestingOnly_AddUnavailability(TimeSlot.Create(fromUtc, toUtc).Value,
            CausedBy.Create(UnavailabilityCausedBy.Reservation, "areference").Value);

        var result =
            await _application.ReleaseCarAvailabilityAsync(_caller.Object, "anorganizationid", "anid", fromUtc,
                toUtc, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<CarRoot>(c =>
            c.Id == "anid"
            && c.Unavailabilities.Count == 0
        ), It.IsAny<CancellationToken>()));
    }
#endif

    [Fact]
    public async Task WhenReleaseCarAvailabilityAsyncNoUnavailability_ThenNothingReleased()
    {
        var car = CarRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        car.Value.SetManufacturer(Manufacturer.Create(2023, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0])
            .Value);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));
        var fromUtc = DateTime.UtcNow.SubtractHours(1);
        var toUtc = fromUtc.AddHours(1);

        var result =
            await _application.ReleaseCarAvailabilityAsync(_caller.Object, "anorganizationid", "anid", fromUtc,
                toUtc, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<CarRoot>(c =>
            c.Id == "anid"
            && c.Unavailabilities.Count == 0
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenReserveCarIfAvailableAsyncWithUnknownCar_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CarRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.ReserveCarIfAvailableAsync(_caller.Object, "anorganizationid", "anid", DateTime.UtcNow,
                DateTime.UtcNow, "areferenceid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenReserveCarIfAvailableAsyncAndUnavailable_ThenReturnsFalse()
    {
        var car = CarRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        car.Value.SetManufacturer(Manufacturer.Create(2023, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0])
            .Value);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));
        var fromUtc = DateTime.UtcNow.AddHours(1);
        var toUtc = fromUtc.AddHours(1);
        car.Value.TestingOnly_AddUnavailability(TimeSlot.Create(fromUtc, toUtc).Value,
            CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result =
            await _application.ReserveCarIfAvailableAsync(_caller.Object, "anorganizationid", "anid", fromUtc,
                toUtc, "areferenceid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<CarRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

    [Fact]
    public async Task WhenReserveCarIfAvailableAsyncAndAvailable_ThenReturnsTrue()
    {
        var car = SetupRegisteredCar();
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));
        var fromUtc = DateTime.UtcNow.AddHours(1);
        var toUtc = fromUtc.AddHours(1);

        var result =
            await _application.ReserveCarIfAvailableAsync(_caller.Object, "anorganizationid", "anid", fromUtc,
                toUtc, "areferenceid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _repository.Verify(rep => rep.SaveAsync(It.Is<CarRoot>(c =>
            c.Id == "anid"
            && (c.Unavailabilities.Count == 1)
            & (c.Unavailabilities[0].Slot!.From == fromUtc)
            & (c.Unavailabilities[0].Slot!.To == toUtc)
            & (c.Unavailabilities[0].CarId! == "anid")
            & (c.Unavailabilities[0].CausedBy!.Reason == UnavailabilityCausedBy.Reservation)
            & (c.Unavailabilities[0].CausedBy!.Reference == "areferenceid")
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllAvailableCarsAsync_ThenReturnsAllAvailableCars()
    {
        _repository.Setup(rep =>
                rep.SearchAllAvailableCarsAsync(It.IsAny<Identifier>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                    It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<IReadOnlyList<Car>, Error>>(new List<Car>
            {
                new()
                {
                    Id = "acarid",
                    LicenseJurisdiction = "ajurisdiction",
                    LicenseNumber = "aplate",
                    ManagerIds = VehicleManagers.Create("amanagerid").Value,
                    ManufactureMake = "amake",
                    ManufactureModel = "amodel",
                    ManufactureYear = 2023,
                    OrganizationId = "anorganizationid",
                    Status = CarStatus.Registered,
                    VehicleOwnerId = "anownerid"
                }
            }));

        var result =
            await _application.SearchAllAvailableCarsAsync(_caller.Object, "anorganizationid", DateTime.UtcNow,
                DateTime.UtcNow, new SearchOptions(), new GetOptions(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("acarid");
        result.Value.Results[0].Manufacturer!.Make.Should().Be("amake");
        result.Value.Results[0].Manufacturer!.Model.Should().Be("amodel");
        result.Value.Results[0].Manufacturer!.Year.Should().Be(2023);
        result.Value.Results[0].Managers!.First().Id.Should().Be("amanagerid");
        result.Value.Results[0].Owner!.Id.Should().Be("anownerid");
        result.Value.Results[0].Plate!.Jurisdiction.Should().Be("ajurisdiction");
        result.Value.Results[0].Plate!.Number.Should().Be("aplate");
        result.Value.Results[0].Status.Should().Be(CarStatus.Registered.ToString());
    }

    [Fact]
    public async Task WhenSearchAllCarsAsync_ThenReturnsAllCars()
    {
        _repository.Setup(rep =>
                rep.SearchAllCarsAsync(It.IsAny<Identifier>(), It.IsAny<SearchOptions>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<IReadOnlyList<Car>, Error>>(new List<Car>
            {
                new()
                {
                    Id = "acarid",
                    LicenseJurisdiction = "ajurisdiction",
                    LicenseNumber = "aplate",
                    ManagerIds = VehicleManagers.Create("amanagerid").Value,
                    ManufactureMake = "amake",
                    ManufactureModel = "amodel",
                    ManufactureYear = 2023,
                    OrganizationId = "anorganizationid",
                    Status = CarStatus.Registered,
                    VehicleOwnerId = "anownerid"
                }
            }));

        var result =
            await _application.SearchAllCarsAsync(_caller.Object, "anorganizationid", new SearchOptions(),
                new GetOptions(), CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].Id.Should().Be("acarid");
        result.Value.Results[0].Manufacturer!.Make.Should().Be("amake");
        result.Value.Results[0].Manufacturer!.Model.Should().Be("amodel");
        result.Value.Results[0].Manufacturer!.Year.Should().Be(2023);
        result.Value.Results[0].Managers!.First().Id.Should().Be("amanagerid");
        result.Value.Results[0].Owner!.Id.Should().Be("anownerid");
        result.Value.Results[0].Plate!.Jurisdiction.Should().Be("ajurisdiction");
        result.Value.Results[0].Plate!.Number.Should().Be("aplate");
        result.Value.Results[0].Status.Should().Be(CarStatus.Registered.ToString());
    }

    [Fact]
    public async Task WhenTakeOfflineCarAsyncWithUnknownCar_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CarRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.TakeOfflineCarAsync(_caller.Object, "anorganizationid", "anid", null, null,
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenTakeOfflineCarAsync_ThenTakesOffline()
    {
        var car = SetupRegisteredCar();
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(car));
        var fromUtc = DateTime.UtcNow.AddHours(1);
        var toUtc = fromUtc.AddHours(1);

        var result =
            await _application.TakeOfflineCarAsync(_caller.Object, "anorganizationid", "anid", fromUtc,
                toUtc, CancellationToken.None);

        result.Should().BeSuccess();
        _repository.Verify(rep => rep.SaveAsync(It.Is<CarRoot>(c =>
            (c.Unavailabilities.Count == 1)
            & (c.Unavailabilities[0].Slot!.From == fromUtc)
            & (c.Unavailabilities[0].Slot!.To == toUtc)
            & (c.Unavailabilities[0].CarId! == "anid")
            & (c.Unavailabilities[0].CausedBy!.Reason == UnavailabilityCausedBy.Offline)
            & (c.Unavailabilities[0].CausedBy!.Reference == null)
        ), It.IsAny<CancellationToken>()));
    }

    private Result<CarRoot, Error> SetupRegisteredCar()
    {
        var car = CarRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        car.Value.SetManufacturer(Manufacturer.Create(2023, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0])
            .Value);
        car.Value.SetOwnership(VehicleOwner.Create("acallerid").Value);
        car.Value.ChangeRegistration(LicensePlate.Create(Jurisdiction.AllowedCountries[0], "aplate").Value);
        return car;
    }
}