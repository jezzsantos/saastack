using Common;
using Common.Extensions;
using Domain.Common.Events;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class CarRootSpec
{
    private readonly CarRoot _car;

    public CarRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        var entityCount = 0;
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity e) =>
            {
                if (e is UnavailabilityEntity)
                {
                    return $"anunavailbilityid{++entityCount}".ToId();
                }

                return "anid".ToId();
            });
        _car = CarRoot.Create(recorder.Object, identifierFactory.Object,
            "anorganizationid".ToId()).Value;
    }

    [Fact]
    public void WhenCreate_ThenUnregistered()
    {
        _car.Status.Should().Be(CarStatus.Unregistered);
    }

    [Fact]
    public void WhenSetManufacturer_ThenManufactured()
    {
        var manufacturer =
            Manufacturer.Create(Year.MinYear + 1, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0]).Value;

        _car.SetManufacturer(manufacturer);

        _car.Manufacturer.Should().Be(manufacturer);
        _car.Events.Last().Should().BeOfType<Events.ManufacturerChanged>();
    }

    [Fact]
    public void WhenSetOwnership_ThenOwnedAndManaged()
    {
        var owner = VehicleOwner.Create("anownerid").Value;
        _car.SetOwnership(owner);

        _car.Owner.Should().Be(VehicleOwner.Create(owner.OwnerId).Value);
        _car.Managers.Managers.Single().Should().Be("anownerid".ToId());
        _car.Events.Last().Should().BeOfType<Events.OwnershipChanged>();
    }

    [Fact]
    public void WhenChangeRegistration_ThenRegistered()
    {
        _car.ChangeRegistration(LicensePlate.Create(Jurisdiction.AllowedCountries[0], "aplate").Value);

        _car.License.Should().Be(LicensePlate.Create(Jurisdiction.AllowedCountries[0], "aplate").Value);
        _car.Status.Should().Be(CarStatus.Registered);
        _car.Events.Last().Should().BeOfType<Events.RegistrationChanged>();
    }

    [Fact]
    public void WhenDeleteAndNotOwned_ThenReturnsError()
    {
        var result = _car.Delete("adeleterid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotOwned);
    }

    [Fact]
    public void WhenDeleteAndNotByOwner_ThenReturnsError()
    {
        _car.SetOwnership(VehicleOwner.Create("anownerid").Value);

        var result = _car.Delete("adeleterid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotDeletedByOwner);
    }

    [Fact]
    public void WhenDeleteByOwner_ThenDeleted()
    {
        _car.SetOwnership(VehicleOwner.Create("anownerid").Value);

        var result = _car.Delete("anownerid".ToId());

        result.Should().BeSuccess();
        _car.Events.Last().Should().BeOfType<Global.StreamDeleted>();
    }

    [Fact]
    public void WhenTakeOfflineInThePast_ThenReturnsError()
    {
        var start = DateTime.UtcNow.SubtractSeconds(1);
        SetupCar();

        var result = _car.TakeOffline(TimeSlot.Create(start, start.AddHours(1)).Value);

        result.Should().BeError(ErrorCode.Validation, Resources.CarRoot_OfflineInPast);
    }

#if TESTINGONLY
    [Fact]
    public void WhenTakeOfflineAndUnavailable_ThenReturnsError()
    {
        var start = DateTime.UtcNow.AddSeconds(1);
        var end = start.AddHours(1);
        SetupCar();
        var slot = TimeSlot.Create(start, end).Value;
        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result = _car.TakeOffline(slot);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_Unavailable);
    }
#endif

    [Fact]
    public void WhenTakeOfflineAndAvailable_ThenOffline()
    {
        var start = DateTime.UtcNow.AddSeconds(1);
        var end = start.AddHours(1);
        SetupCar();
        var slot = TimeSlot.Create(start, end).Value;

        _car.TakeOffline(slot);

        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot);
        _car.Unavailabilities[0].CausedBy!.Reason.Should().Be(UnavailabilityCausedBy.Offline);
        _car.Unavailabilities[0].CausedBy!.Reference.Should().BeNull();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }

    [Fact]
    public void WhenScheduleMaintenanceInThePast_ThenReturnsError()
    {
        var start = DateTime.UtcNow.Add(Validations.Car.MinScheduledMaintenanceLeadTime).SubtractSeconds(1);
        SetupCar();

        var result = _car.ScheduleMaintenance(TimeSlot.Create(start, start.AddHours(1)).Value);

        result.Should().BeError(ErrorCode.Validation,
            Resources.CarRoot_ScheduleMaintenanceLessThanMinimumLeadTime.Format(Validations.Car
                .MinScheduledMaintenanceLeadTime.TotalHours));
    }

#if TESTINGONLY
    [Fact]
    public void WhenScheduleMaintenanceAndUnavailable_ThenReturnsError()
    {
        var start = DateTime.UtcNow.Add(Validations.Car.MinScheduledMaintenanceLeadTime).AddSeconds(1);
        var end = start.AddHours(1);
        SetupCar();
        var slot = TimeSlot.Create(start, end).Value;
        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result = _car.ScheduleMaintenance(slot);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_Unavailable);
    }
#endif

    [Fact]
    public void WhenScheduleMaintenanceAndAvailable_ThenOffline()
    {
        var start = DateTime.UtcNow.Add(Validations.Car.MinScheduledMaintenanceLeadTime).AddSeconds(1);
        var end = start.AddHours(1);
        SetupCar();
        var slot = TimeSlot.Create(start, end).Value;

        _car.ScheduleMaintenance(slot);

        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot);
        _car.Unavailabilities[0].CausedBy!.Reason.Should().Be(UnavailabilityCausedBy.Maintenance);
        _car.Unavailabilities[0].CausedBy!.Reference.Should().BeNull();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }

    [Fact]
    public void WhenReserveIfAvailableInThePast_ThenReturnsError()
    {
        var start = DateTime.UtcNow.SubtractSeconds(1);
        var end = start.AddHours(1);
        SetupCar();

        var result = _car.ReserveIfAvailable(TimeSlot.Create(start, end).Value, "areference");

        result.Should().BeError(ErrorCode.Validation, Resources.CarRoot_ReserveInPast);
    }

#if TESTINGONLY
    [Fact]
    public void WhenReserveIfAvailableAndUnavailable_ThenReturnsFalse()
    {
        var start = DateTime.UtcNow.AddSeconds(1);
        var end = start.AddHours(1);
        SetupCar();
        var slot = TimeSlot.Create(start, end).Value;
        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result = _car.ReserveIfAvailable(slot, "areference").Value;

        result.Should().BeFalse();
        _car.Unavailabilities.Count.Should().Be(1);
    }
#endif

    [Fact]
    public void WhenReserveIfAvailableAndAvailable_ThenReturnsTrue()
    {
        var start = DateTime.UtcNow.AddSeconds(1);
        var end = start.AddHours(1);
        SetupCar();
        var slot = TimeSlot.Create(start, end).Value;

        var result = _car.ReserveIfAvailable(slot, "areference").Value;

        result.Should().BeTrue();
        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot);
        _car.Unavailabilities[0].CausedBy!.Reason.Should().Be(UnavailabilityCausedBy.Reservation);
        _car.Unavailabilities[0].CausedBy!.Reference.Should().Be("areference");
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }

    [Fact]
    public void WhenReleaseAvailabilityAndNoUnavailabilities_ThenDoesNothing()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        SetupCar();

        _car.ReleaseUnavailability(TimeSlot.Create(start, end).Value);

        _car.Unavailabilities.Count.Should().Be(0);
    }

#if TESTINGONLY
    [Fact]
    public void WhenReleaseAvailability_ThenRemovesUnavailability()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        SetupCar();
        _car.TestingOnly_AddUnavailability(TimeSlot.Create(start, end).Value,
            CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        _car.ReleaseUnavailability(TimeSlot.Create(start, end).Value);

        _car.Unavailabilities.Count.Should().Be(0);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailabilityAndNotManufactured_ThenReturnsError()
    {
        _car.SetOwnership(VehicleOwner.Create("anownerid").Value);
        _car.ChangeRegistration(LicensePlate.Create(Jurisdiction.AllowedCountries[0], "aplate").Value);

        var result = _car.TestingOnly_AddUnavailability(
            TimeSlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddSeconds(1)).Value,
            CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotManufactured);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailabilityAndNotOwned_ThenReturnsError()
    {
        _car.SetManufacturer(Manufacturer
            .Create(Year.MinYear + 1, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0]).Value);
        _car.ChangeRegistration(LicensePlate.Create(Jurisdiction.AllowedCountries[0], "aplate").Value);

        var result = _car.TestingOnly_AddUnavailability(
            TimeSlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddSeconds(1)).Value,
            CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotOwned);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailabilityAndNotRegistered_ThenReturnsError()
    {
        _car.SetManufacturer(Manufacturer
            .Create(Year.MinYear + 1, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0]).Value);
        _car.SetOwnership(VehicleOwner.Create("anownerid").Value);

        var result = _car.TestingOnly_AddUnavailability(
            TimeSlot.Create(DateTime.UtcNow, DateTime.UtcNow.AddSeconds(1)).Value,
            CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotRegistered);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailability_ThenUnavailable()
    {
        var from = DateTime.UtcNow.AddDays(1);
        var to = from.AddDays(1);
        var slot = TimeSlot.Create(from, to).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot);
        _car.Unavailabilities[0].CausedBy!.Reason.Should().Be(UnavailabilityCausedBy.Other);
        _car.Unavailabilities[0].CausedBy!.Reference.Should().BeNull();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
        _car.Events.Last().As<Events.UnavailabilitySlotAdded>().UnavailabilityId.Should()
            .Be("anunavailbilityid1");
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableAndNotExist_ThenCreatesUnavailability()
    {
        var datum = DateTime.UtcNow;
        var slot = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot);
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithIntersectingSlotWithSameCauseNoReference_ThenReplacesEntity()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum, datum.AddMinutes(5)).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        _car.TestingOnly_AddUnavailability(slot2, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot2);
        _car.Events[4].Should().BeOfType<Events.UnavailabilitySlotAdded>();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithIntersectingSlotWithSameCauseSameReference_ThenReplacesEntity()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum, datum.AddMinutes(5)).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, "areference").Value);
        _car.TestingOnly_AddUnavailability(slot2, CausedBy.Create(UnavailabilityCausedBy.Other, "areference").Value);

        _car.Unavailabilities.Count.Should().Be(1);
        _car.Unavailabilities[0].Slot.Should().Be(slot2);
        _car.Events[4].Should().BeOfType<Events.UnavailabilitySlotAdded>();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithIntersectingSlotWithDifferentCauseNoReference_ThenReturnsError()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum, datum.AddMinutes(5)).Value;
        SetupCar();
        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        var result =
            _car.TestingOnly_AddUnavailability(slot2, CausedBy.Create(UnavailabilityCausedBy.Offline, null).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Unavailabilities_OverlappingSlot);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithIntersectingSlotWithSameCauseDifferentReference_ThenReturnsError()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum, datum.AddMinutes(5)).Value;
        SetupCar();
        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);

        var result = _car.TestingOnly_AddUnavailability(slot2,
            CausedBy.Create(UnavailabilityCausedBy.Other, "areference2").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Unavailabilities_OverlappingSlot);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithIntersectingSlotWithDifferentCauseDifferentReference_ThenReturnsError()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum, datum.AddMinutes(5)).Value;
        SetupCar();
        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);

        var result = _car.TestingOnly_AddUnavailability(slot2,
            CausedBy.Create(UnavailabilityCausedBy.Offline, "areference2").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Unavailabilities_OverlappingSlot);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithNotIntersectingSlotSameCause_ThenAddsUnavailability()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum.AddMinutes(5), datum.AddMinutes(10)).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        _car.TestingOnly_AddUnavailability(slot2, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);

        _car.Unavailabilities.Count.Should().Be(2);
        _car.Unavailabilities[0].Slot.Should().Be(slot1);
        _car.Unavailabilities[1].Slot.Should().Be(slot2);
        _car.Events[4].Should().BeOfType<Events.UnavailabilitySlotAdded>();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenAddUnavailableWithNotIntersectingSlotDifferentCause_ThenAddsUnavailability()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum.AddMinutes(5), datum.AddMinutes(10)).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, null).Value);
        _car.TestingOnly_AddUnavailability(slot2, CausedBy.Create(UnavailabilityCausedBy.Offline, null).Value);

        _car.Unavailabilities.Count.Should().Be(2);
        _car.Unavailabilities[0].Slot.Should().Be(slot1);
        _car.Unavailabilities[1].Slot.Should().Be(slot2);
        _car.Events[4].Should().BeOfType<Events.UnavailabilitySlotAdded>();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void
        WhenAddUnavailableWithNotIntersectingSlotDifferentCauseAndDifferentReference_ThenAddsUnavailability()
    {
        var datum = DateTime.UtcNow;
        var slot1 = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        var slot2 = TimeSlot.Create(datum.AddMinutes(5), datum.AddMinutes(10)).Value;
        SetupCar();

        _car.TestingOnly_AddUnavailability(slot1, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
        _car.TestingOnly_AddUnavailability(slot2, CausedBy.Create(UnavailabilityCausedBy.Offline, "areference2").Value);

        _car.Unavailabilities.Count.Should().Be(2);
        _car.Unavailabilities[0].Slot.Should().Be(slot1);
        _car.Unavailabilities[1].Slot.Should().Be(slot2);
        _car.Events[4].Should().BeOfType<Events.UnavailabilitySlotAdded>();
        _car.Events.Last().Should().BeOfType<Events.UnavailabilitySlotAdded>();
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenEnsureInvariantsAndHasUnavailabilityButNoManufacturer_ThenReturnsError()
    {
        SetupCar();
        var datum = DateTime.UtcNow;
        var slot = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
        _car.TestingOnly_ResetDetails(Optional<Manufacturer>.None, _car.Owner, _car.License);

        var result = _car.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotManufactured);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenEnsureInvariantsAndHasUnavailabilityButNoOwner_ThenReturnsError()
    {
        SetupCar();
        var datum = DateTime.UtcNow;
        var slot = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
        _car.TestingOnly_ResetDetails(_car.Manufacturer, Optional<VehicleOwner>.None, _car.License);

        var result = _car.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotOwned);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenEnsureInvariantsAndHasUnavailabilityButNoPlate_ThenReturnsError()
    {
        SetupCar();
        var datum = DateTime.UtcNow;
        var slot = TimeSlot.Create(datum, datum.AddMinutes(1)).Value;
        _car.TestingOnly_AddUnavailability(slot, CausedBy.Create(UnavailabilityCausedBy.Other, "areference1").Value);
        _car.TestingOnly_ResetDetails(_car.Manufacturer, _car.Owner, Optional<LicensePlate>.None);

        var result = _car.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.CarRoot_NotRegistered);
    }
#endif

    private void SetupCar()
    {
        _car.SetManufacturer(Manufacturer
            .Create(Year.MinYear + 1, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0]).Value);
        _car.SetOwnership(VehicleOwner.Create("anownerid").Value);
        _car.ChangeRegistration(LicensePlate.Create(Jurisdiction.Create(Jurisdiction.AllowedCountries[0]).Value,
            NumberPlate.Create("aplate").Value).Value);
    }
}