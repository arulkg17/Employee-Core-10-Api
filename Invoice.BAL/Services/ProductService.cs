using AutoMapper;
using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.BAL.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _product;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository product, IMapper mapper)
    {
        _product = product;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var entities = await _product.GetAllAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(entities);
        //return entities.Select(e => new ProductDto {
        //    Id = e.Id,
        //    Name = e.Name,
        //    Description = e.Description,
        //    Price = e.Price,
        //    Stock = e.Stock,
        //});
    }
    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var e = await _product.GetByIdAsync(id);
        if (e == null) return null;
        return _mapper.Map<ProductDto?>(e);
        //return new ProductDto {
        //    Id = e.Id,
        //    Name = e.Name,
        //    Description = e.Description,
        //    Price = e.Price,
        //    Stock = e.Stock,
        //};
    }

    public async Task<ProductDto> CreateAsync(ProductDto dto)
    {
        var e = _mapper.Map<ProductEntity>(dto);

        //var e = new ProductEntity
        //{
        //    Name = dto.Name,
        //    Description = dto.Description,
        //    Price = dto.Price,
        //    Stock = dto.Stock
        //};

        var created = await _product.CreateAsync(e);
        dto = _mapper.Map<ProductDto>(created);
        //dto.Id = created.Id;
        return dto;
    }

    public async Task<bool> UpdateAsync(ProductDto dto)
    {
        var e = _mapper.Map<ProductEntity>(dto);
        //var e = new ProductEntity
        //{
        //    Id = dto.Id,
        //    Name = dto.Name,
        //    Description = dto.Description,
        //    Price = dto.Price,
        //    Stock = dto.Stock
        //};

        return await _product.UpdateAsync(e);
    }

    public async Task<bool> DeleteAsync(int id, string rowVersion)
    {
        var versionBytes = Convert.FromBase64String(rowVersion);
        return await _product.DeleteAsync(id, versionBytes);
    }
}