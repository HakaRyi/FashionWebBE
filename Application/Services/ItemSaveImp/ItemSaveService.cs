using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Response.ItemResp;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;

namespace Application.Services.ItemSaveImp
{
    public class ItemSaveService : IItemSaveService
    {
        private readonly IItemSaveRepository _itemSaveRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        public ItemSaveService(IItemSaveRepository itemSaveRepo, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _itemSaveRepo = itemSaveRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task DeleteSaveItem(int itemId)
        {
            var accId = _currentUserService.GetUserId()??0;
            if (accId == 0)
            {
                throw new Exception("User not authenticated.");
            }
            var savedItem = await _itemSaveRepo.GetSaveItem(itemId, accId);
            if (savedItem == null)
            {
                throw new Exception("Saved item not found.");
            }
            await _itemSaveRepo.DeleteSaveItem(savedItem);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<ItemResponseDto>> GetMySaveItems()
        {
            var accId = _currentUserService.GetUserId()??0;
            if (accId == 0)
            {
                throw new Exception("User not authenticated.");
            }
            var savedItems = await _itemSaveRepo.GetMySaveItems(accId);
            return savedItems.Adapt<List<ItemResponseDto>>();

        }

        public async Task SaveItem(int itemId)
        {
            var userId = _currentUserService.GetUserId()??0;
            if (userId == 0)
            {
                throw new Exception("User not authenticated.");
            }
            var existingSave = await _itemSaveRepo.GetSaveItem(itemId, userId);
            if (existingSave != null)
            {
                throw new Exception("Item already saved.");
            }
            var savedItem = new SavedItem
            {
                AccountId = userId,
                ItemId = itemId,
            };
            await _itemSaveRepo.SaveItem(savedItem);
            await _unitOfWork.CommitAsync();

        }
    }
}
