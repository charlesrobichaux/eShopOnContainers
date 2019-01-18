namespace Microsoft.eShopOnContainers.Services.Locations.API.Infrastructure.Services
{
    using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
    using Microsoft.eShopOnContainers.Services.Locations.API.Infrastructure.Exceptions;
    using Microsoft.eShopOnContainers.Services.Locations.API.Infrastructure.Repositories;
    using Microsoft.eShopOnContainers.Services.Locations.API.IntegrationEvents.Events;
    using Microsoft.eShopOnContainers.Services.Locations.API.Model;
    using Microsoft.eShopOnContainers.Services.Locations.API.ViewModel;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus;
    public class LocationsService : ILocationsService
    {
        private readonly ILocationsRepository _locationsRepository;
        //private readonly IEventBus _eventBus;
        private readonly IEndpointInstance _endpoint;

        public LocationsService(ILocationsRepository locationsRepository, IEventBus eventBus, IEndpointInstance endpoint)
        {
            _locationsRepository = locationsRepository ?? throw new ArgumentNullException(nameof(locationsRepository));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            //_eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<Locations> GetLocationAsync(int locationId) => 
            await _locationsRepository.GetAsync(locationId).ConfigureAwait(false);

        public async Task<UserLocation> GetUserLocationAsync(string userId) =>
            await _locationsRepository.GetUserLocationAsync(userId).ConfigureAwait(false);

        public async Task<List<Locations>> GetAllLocationAsync() => 
            await _locationsRepository.GetLocationListAsync().ConfigureAwait(false);

        public async Task<bool> AddOrUpdateUserLocationAsync(string userId, LocationRequest currentPosition)
        {            
            // Get the list of ordered regions the user currently is within
            var currentUserAreaLocationList = await _locationsRepository.GetCurrentUserRegionsListAsync(currentPosition).ConfigureAwait(false);
                      
            if(currentUserAreaLocationList is null)
            {
                throw new LocationDomainException("User current area not found");
            }

            // If current area found, then update user location
            var locationAncestors = new List<string>();
            var userLocation = await _locationsRepository.GetUserLocationAsync(userId).ConfigureAwait(false);
            userLocation = userLocation ?? new UserLocation();
            userLocation.UserId = userId;
            userLocation.LocationId = currentUserAreaLocationList[0].LocationId;
            userLocation.UpdateDate = DateTime.UtcNow;
            await _locationsRepository.UpdateUserLocationAsync(userLocation).ConfigureAwait(false);

            // Publish integration event to update marketing read data model
            // with the new locations updated
            await PublishNewUserLocationPositionIntegrationEvent(userId, currentUserAreaLocationList).ConfigureAwait(false);

            return true;
        }

        private async Task PublishNewUserLocationPositionIntegrationEvent(string userId, List<Locations> newLocations)
        {
            var newUserLocations = MapUserLocationDetails(newLocations);
            var @event = new UserLocationUpdatedIntegrationEvent(userId, newUserLocations);
            await _endpoint.Publish(@event).ConfigureAwait(false);
        }

        private static List<UserLocationDetails> MapUserLocationDetails(List<Locations> newLocations)
        {
            var result = new List<UserLocationDetails>();
            newLocations.ForEach(location => {
                result.Add(new UserLocationDetails()
                {
                    LocationId = location.LocationId,
                    Code = location.Code,
                    Description = location.Description
                });
            });

            return result;
        }
    }
}
