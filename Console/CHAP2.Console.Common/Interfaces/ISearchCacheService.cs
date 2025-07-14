using System;
using System.Collections.Generic;
using CHAP2.Common.Models;

namespace CHAP2.Console.Common.Interfaces;

public interface ISearchCacheService
{
    bool TryGet(string searchTerm, out List<Chorus> results);
    void Set(string searchTerm, List<Chorus> results, TimeSpan duration);
} 