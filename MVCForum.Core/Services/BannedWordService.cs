﻿namespace MvcForum.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Threading.Tasks;
    using Constants;
    using Data.Context;
    using Interfaces;
    using Interfaces.Services;
    using Models.Entities;
    using Models.General;
    using Utilities;

    public partial class BannedWordService : IBannedWordService
    {
        private readonly IMvcForumContext _context;
        private readonly ICacheService _cacheService;

        public BannedWordService(IMvcForumContext context, ICacheService cacheService)
        {
            _cacheService = cacheService;
            _context = context;
        }

        public BannedWord Add(BannedWord bannedWord)
        {
            return _context.BannedWord.Add(bannedWord);
        }

        public void Delete(BannedWord bannedWord)
        {
            _context.BannedWord.Remove(bannedWord);
        }

        public IList<BannedWord> GetAll(bool onlyStopWords = false)
        {
            var cacheKey = string.Concat(CacheKeys.BannedWord.StartsWith, "GetAll-", onlyStopWords);
            return _cacheService.CachePerRequest(cacheKey, () =>
            {
                if (onlyStopWords)
                {
                    return _context.BannedWord.AsNoTracking().Where(x => x.IsStopWord == true).OrderByDescending(x => x.DateAdded).ToList();
                }
                return _context.BannedWord.AsNoTracking().Where(x => x.IsStopWord != true).OrderByDescending(x => x.DateAdded).ToList();
            });
        }

        public BannedWord Get(Guid id)
        {
            var cacheKey = string.Concat(CacheKeys.BannedWord.StartsWith, "Get-", id);
            return _cacheService.CachePerRequest(cacheKey, () => _context.BannedWord.FirstOrDefault(x => x.Id == id));
        }

        public async Task<PaginatedList<BannedWord>> GetAllPaged(int pageIndex, int pageSize)
        {
            var query = _context.BannedWord.OrderBy(x => x.Word);
            return await PaginatedList<BannedWord>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);
        }

        public async Task<PaginatedList<BannedWord>> GetAllPaged(string search, int pageIndex, int pageSize)
        {
            search = StringUtils.SafePlainText(search);
            var query = _context.BannedWord
                            .Where(x => x.Word.ToLower().Contains(search.ToLower()))
                            .OrderBy(x => x.Word);
            return await PaginatedList<BannedWord>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

        }

        public string SanitiseBannedWords(string content)
        {
            var bannedWords = GetAll();
            if (bannedWords.Any())
            {
                return SanitiseBannedWords(content, bannedWords.Select(x => x.Word).ToList());
            }
            return content;
        }

        public string SanitiseBannedWords(string content, IList<string> words)
        {
            if (words != null && words.Any() && !string.IsNullOrWhiteSpace(content))
            {
                var censor = new CensorUtils(words);
                return censor.CensorText(content);
            }
            return content;
        }

        /// <inheritdoc />
        public bool ContainsStopWords(string content, IList<string> words)
        {
            if (words != null && words.Any() && !string.IsNullOrWhiteSpace(content))
            {
                foreach (var word in words)
                {
                    if (content.ContainsCaseInsensitive(word))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
