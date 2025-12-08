using AutoMapper;
using EVWebApi.DTOs.Cabinet;
using EVWebApi.DTOs.Document;
using EVWebApi.DTOs.Pagination;
using EVWebApi.DTOs.User;
using EVWebApi.Exceptions;
using EVWebApi.Interfaces.Repositories;
using EVWebApi.Interfaces.Services;
using EVWebApi.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EVWebApi.Services
{
    public class CabinetService : ICabinetService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;



        public CabinetService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<PagedResponse<CabinetDto>> GetAllAsync(CabinetQueryParameters query)
        {
            var cabinetsQuery = _uow.Cabinets.Query();
            if (!string.IsNullOrWhiteSpace(query.CabinetName))
            {
                string cabinetname = query.CabinetName.ToLower();
                cabinetsQuery = cabinetsQuery.Where(g =>
                    g.CabinetName.ToLower().Contains(cabinetname)
                );
            }

            //  Date Range
            if (query.FromDate.HasValue)
            {
                cabinetsQuery = cabinetsQuery.Where(g => g.CreatedAt >= query.FromDate.Value);
            }
            if (query.ToDate.HasValue)
            {
                cabinetsQuery = cabinetsQuery.Where(g => g.CreatedAt <= query.ToDate.Value);
            }


        var totalRecords = await cabinetsQuery.CountAsync();

        // If pageSize is invalid, normalize it
            if (query.PageSize <= 0)
                query.PageSize = 10;

            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalRecords / (double)query.PageSize);

            // Normalize pageNumber
            if (query.PageNumber <= 0)
                query.PageNumber = 1;

            if (query.PageNumber > totalPages && totalPages > 0)
                query.PageNumber = totalPages;
            // APPLY PAGINATION
            var pagedCabinets = cabinetsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            // MAP TO DTO
            var cabinetDtos = _mapper.Map<List<CabinetDto>>(pagedCabinets);
            return new PagedResponse<CabinetDto>
            {
                Data = cabinetDtos,
                TotalRecords = totalRecords,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<CabinetDto> GetByIdAsync(int id)
        {
            var cabinets = await _uow.Cabinets.GetByIdAsync(id);
            if (cabinets == null)
                throw new NotFoundException($"Cabinet with id {id} not found");

            return _mapper.Map<CabinetDto>(cabinets);
        }

        public async Task<CabinetDto> CreateAsync(CreateCabinetDto dto)
        {
            var exists = await _uow.Cabinets.GetByCabinetnameAsync(dto.CabinetName);
            if (exists != null)
                throw new ConflictException($"Cabinet name '{dto.CabinetName}' already exists");

            var cabinet = new Cabinet
            {
                CabinetName=dto.CabinetName,
                Description=dto.Description
            };
            await _uow.Cabinets.AddAsync(cabinet);
            await _uow.CompleteAsync();
            return _mapper.Map<CabinetDto>(cabinet);
        }

        public async Task<CabinetDto> UpdateAsync(UpdateCabinetDto dto)
        {
            var cabinet =await _uow.Cabinets.GetByIdAsync(dto.CabinetId);
            if (cabinet == null)
                throw new NotFoundException($"Cabinet with id {dto.CabinetId} not found");

            if (!string.IsNullOrWhiteSpace(dto.CabinetName)) cabinet.CabinetName = dto.CabinetName;
            if (!string.IsNullOrWhiteSpace(dto.Description)) cabinet.Description = dto.Description;

            _uow.Cabinets.Update(cabinet);
            await _uow.CompleteAsync();
            return _mapper.Map<CabinetDto>(cabinet);
        }

        public async Task DeleteAsync(int id)
        {
            var cabinet = await _uow.Cabinets.GetByIdAsync(id);
            if (cabinet == null)
                throw new NotFoundException("Cabinet not found");


            _uow.Cabinets.Remove(cabinet);
            await _uow.CompleteAsync();
        }
    }
}