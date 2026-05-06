using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Request.CollectionDto;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;

namespace Application.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICollectionRepository _collectionRepository;
        private readonly ICurrentUserService _currentUserService;
        public CollectionService(IUnitOfWork unitOfWork, ICollectionRepository collectionRepository, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _collectionRepository = collectionRepository;
            _currentUserService = currentUserService;
        }
        public async Task<bool> DeleteCollectionAsync(int collectionId)
        {
            var userId = _currentUserService.GetRequiredUserId();
            var collection = await _collectionRepository.GetByIdAsync(collectionId, userId);
            await _collectionRepository.DeleteAsync(collection);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<List<CollectionResponseDto>> GetUserCollectionsAsync()
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var list = await _collectionRepository.GetByAccountIdAsync(accountId);
            return list.Adapt<List<CollectionResponseDto>>();
        }

        public async Task SaveCollectionAsync(CollectionCreateDto dto)
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var collection = new Collection
            {
                AccountId = accountId,
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                CollectionItems = dto.ItemIds.Select(itemId => new CollectionItem
                {
                    ItemId = itemId,
                    AddedAt = DateTime.UtcNow
                }).ToList()
            };

            await _collectionRepository.CreateAsync(collection);
            await _unitOfWork.CommitAsync();

        }
        public async Task<bool> AddItemsToCollectionAsync(int collectionId, List<int> itemIdsToAdd)
        {
            var userId = _currentUserService.GetRequiredUserId();
            var collection = await _collectionRepository.GetByIdAsync(collectionId, userId);
            if (collection == null) return false;

            var currentItemIds = collection.CollectionItems.Select(ci => ci.ItemId).ToList();

            var idsToAdd = itemIdsToAdd.Distinct().Where(id => !currentItemIds.Contains(id)).ToList();

            foreach (var itemId in idsToAdd)
            {
                collection.CollectionItems.Add(new CollectionItem
                {
                    CollectionId = collectionId,
                    ItemId = itemId,
                    AddedAt = DateTime.UtcNow
                });
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }
        public async Task<bool> RemoveItemsFromCollectionAsync(int collectionId, List<int> itemIdsToRemove)
        {
            var userId = _currentUserService.GetRequiredUserId();
            var collection = await _collectionRepository.GetByIdAsync(collectionId, userId);
            if (collection == null) return false;

            var itemsToRemove = collection.CollectionItems
                .Where(ci => itemIdsToRemove.Contains(ci.ItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                collection.CollectionItems.Remove(item);
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }
        public async Task<bool> UpdateCollectionItemsAsync(int collectionId, CollectionUpdateDto dto)
        {
            var userId = _currentUserService.GetRequiredUserId();

            var collection = await _collectionRepository.GetByIdAsync(collectionId, userId);
            if (collection == null) return false;

            var currentItemIds = collection.CollectionItems.Select(ci => ci.ItemId).ToList();

            //handle remove
            var itemsToRemove = collection.CollectionItems
                .Where(ci => !dto.NewItemIds.Contains(ci.ItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                collection.CollectionItems.Remove(item);
            }

            //handle add
            var idsToAdd = dto.NewItemIds
                .Distinct()
                .Where(id => !currentItemIds.Contains(id))
                .ToList();

            foreach (var itemId in idsToAdd)
            {
                collection.CollectionItems.Add(new CollectionItem
                {
                    CollectionId = collectionId,
                    ItemId = itemId,
                    AddedAt = DateTime.UtcNow
                });
            }
            
            collection.Title = dto.NewTitle;
            collection.Description = dto.NewDescription;
            await _collectionRepository.UpdateAsync(collection);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
