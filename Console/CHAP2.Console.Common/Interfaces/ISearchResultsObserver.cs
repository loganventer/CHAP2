namespace CHAP2.Console.Common.Interfaces;

using System.Collections.Generic;
using CHAP2.Common.Models;

public interface ISearchResultsObserver
{
    void OnResultsChanged(List<Chorus> results, string searchTerm);
} 