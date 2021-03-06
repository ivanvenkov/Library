﻿using AutoMapper;
using Data.Models;
using LibraryAPI.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryAPI
{
    public class AuthorService : IAuthorService
    {
        private readonly ModelContext dbContext;
        private readonly IMapper autoMapper;

        public AuthorService(ModelContext dbContext, IMapper autoMapper)
        {
            this.dbContext = dbContext;
            this.autoMapper = autoMapper;
        }

        public async Task<IEnumerable<AuthorVM>> GetAuthors()
        {
            var allAuthors = await dbContext.Authors.Include(x => x.Book).ToListAsync();
            var result = this.autoMapper.Map<List<AuthorVM>>(allAuthors);
            return result;
        }

        public async Task<AuthorVM> GetAuthorById(int id)
        {
            var currentAuthor = await this.dbContext.Authors.FirstOrDefaultAsync(a => a.AuthorId == id);
            var currentBook = await this.dbContext.Books.FindAsync(currentAuthor.BookTableId);

            if (currentBook == null)
            {
                throw new ArgumentException("Author doesn't have any book!");
            }

            var resultAuthor = this.autoMapper.Map<AuthorVM>(currentAuthor);
            return resultAuthor;
        }

        public async Task<AuthorVM> Add(AuthorVM author)
        {
            var authorExists = await dbContext.Authors.Select(x => x.AuthorName).ContainsAsync(author.AuthorName);

            if (authorExists)
            {
                throw new ArgumentException($"Author with name {author.AuthorName} already exists in db!");
            }

            var authorId = await GetNextValue();

            var currentBook = await this.dbContext.Books.FindAsync(author.BookTableId);

            if (currentBook == null)
            {
                throw new ArgumentException($"The book with id {author.BookTableId} doesn't exist!");
            }

            var newAuthor = this.autoMapper.Map<AuthorTable>(author);
            newAuthor.AuthorId = authorId;
            currentBook.AuthorTables = new List<AuthorTable> { newAuthor };
            await this.dbContext.Authors.AddAsync(newAuthor);

            await dbContext.SaveChangesAsync();

            var resultAsVM = this.autoMapper.Map<AuthorVM>(newAuthor);

            return resultAsVM;
        }

        public async Task Delete(int id)
        {
            var currentAuthor = await this.dbContext.Authors.FirstOrDefaultAsync(a => a.AuthorId == id);

            if (currentAuthor == null)
            {
                throw new ArgumentNullException($"Author with ID {id} doesn't exist!");
            }

            this.dbContext.Authors.Remove(currentAuthor);
            await dbContext.SaveChangesAsync();
        }

        public async Task Update(int id, AuthorVM author)
        {
            var currentAuthor = await this.dbContext.Authors.FirstOrDefaultAsync(a => a.AuthorId == id);
            if (currentAuthor == null)
            {
                throw new ArgumentNullException($"Author with ID {id} doesn't exist!");
            }

            currentAuthor.AuthorName = author.AuthorName;
            currentAuthor.BookTableId = author.BookTableId;

            await dbContext.SaveChangesAsync();
        }

        public async Task<int> GetNextValue()
        {
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = $"select authors_next_id.NEXTVAL from dual";
                await dbContext.Database.OpenConnectionAsync();

                using (var reader = command.ExecuteReader())
                {
                    await reader.ReadAsync();
                    return reader.GetInt32(0);
                }
            }
        }
    }
}

