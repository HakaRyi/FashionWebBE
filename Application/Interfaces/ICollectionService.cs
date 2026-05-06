using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Request.CollectionDto;

namespace Application.Interfaces
{
    public interface ICollectionService
    {
        Task SaveCollectionAsync(CollectionCreateDto dto);
        Task<List<CollectionResponseDto>> GetUserCollectionsAsync();
        Task<bool> DeleteCollectionAsync(int collectionId);
        Task<bool> UpdateCollectionItemsAsync(int collectionId, CollectionUpdateDto dto);
        Task<bool> AddItemsToCollectionAsync(int collectionId, List<int> itemIdsToAdd);
        Task<bool> RemoveItemsFromCollectionAsync(int collectionId, List<int> itemIdsToRemove);
    }
}
