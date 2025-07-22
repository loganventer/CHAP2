using CHAP2.WebPortal.Configuration;
using CHAP2.WebPortal.DTOs;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CHAP2.WebPortal.Services;

public class VectorSearchService : IVectorSearchService
{
    private readonly QdrantSettings _settings;
    private readonly ILogger<VectorSearchService> _logger;
    private QdrantClient? _client;
    private const int VECTOR_DIMENSION = 1536;
    
    // Enhanced vocabulary with RAG-optimized features (same as console app)
    private static readonly Dictionary<string, int[]> WordPositions = new()
    {
        // Core religious terms (highest priority for RAG)
        ["jesus"] = new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
        ["christ"] = new[] { 8, 9, 10, 11, 12, 13, 14, 15 },
        ["god"] = new[] { 16, 17, 18, 19, 20, 21, 22, 23 },
        ["lord"] = new[] { 24, 25, 26, 27, 28, 29, 30, 31 },
        ["heer"] = new[] { 32, 33, 34, 35, 36, 37, 38, 39 },
        ["holy"] = new[] { 40, 41, 42, 43, 44, 45, 46, 47 },
        ["spirit"] = new[] { 48, 49, 50, 51, 52, 53, 54, 55 },
        
        // Worship and praise terms
        ["praise"] = new[] { 56, 57, 58, 59, 60, 61, 62, 63 },
        ["worship"] = new[] { 64, 65, 66, 67, 68, 69, 70, 71 },
        ["prys"] = new[] { 72, 73, 74, 75, 76, 77, 78, 79 },
        ["aanbid"] = new[] { 80, 81, 82, 83, 84, 85, 86, 87 },
        ["verheerlik"] = new[] { 88, 89, 90, 91, 92, 93, 94, 95 },
        ["glorify"] = new[] { 96, 97, 98, 99, 100, 101, 102, 103 },
        ["exalt"] = new[] { 104, 105, 106, 107, 108, 109, 110, 111 },
        ["magnify"] = new[] { 112, 113, 114, 115, 116, 117, 118, 119 },
        
        // Salvation and redemption
        ["salvation"] = new[] { 120, 121, 122, 123, 124, 125, 126, 127 },
        ["redemption"] = new[] { 128, 129, 130, 131, 132, 133, 134, 135 },
        ["verlossing"] = new[] { 136, 137, 138, 139, 140, 141, 142, 143 },
        ["verlosser"] = new[] { 144, 145, 146, 147, 148, 149, 150, 151 },
        ["savior"] = new[] { 152, 153, 154, 155, 156, 157, 158, 159 },
        ["redeemer"] = new[] { 160, 161, 162, 163, 164, 165, 166, 167 },
        
        // Grace and mercy
        ["grace"] = new[] { 168, 169, 170, 171, 172, 173, 174, 175 },
        ["mercy"] = new[] { 176, 177, 178, 179, 180, 181, 182, 183 },
        ["genade"] = new[] { 184, 185, 186, 187, 188, 189, 190, 191 },
        ["barmhartigheid"] = new[] { 192, 193, 194, 195, 196, 197, 198, 199 },
        ["kindness"] = new[] { 200, 201, 202, 203, 204, 205, 206, 207 },
        ["gunst"] = new[] { 208, 209, 210, 211, 212, 213, 214, 215 },
        
        // Faith and trust
        ["faith"] = new[] { 216, 217, 218, 219, 220, 221, 222, 223 },
        ["trust"] = new[] { 224, 225, 226, 227, 228, 229, 230, 231 },
        ["geloof"] = new[] { 232, 233, 234, 235, 236, 237, 238, 239 },
        ["vertroue"] = new[] { 240, 241, 242, 243, 244, 245, 246, 247 },
        ["hope"] = new[] { 248, 249, 250, 251, 252, 253, 254, 255 },
        ["hoop"] = new[] { 256, 257, 258, 259, 260, 261, 262, 263 },
        
        // Prayer and supplication
        ["prayer"] = new[] { 264, 265, 266, 267, 268, 269, 270, 271 },
        ["pray"] = new[] { 272, 273, 274, 275, 276, 277, 278, 279 },
        ["gebed"] = new[] { 280, 281, 282, 283, 284, 285, 286, 287 },
        ["smeking"] = new[] { 288, 289, 290, 291, 292, 293, 294, 295 },
        ["supplication"] = new[] { 296, 297, 298, 299, 300, 301, 302, 303 },
        
        // Love and devotion
        ["love"] = new[] { 304, 305, 306, 307, 308, 309, 310, 311 },
        ["liefde"] = new[] { 312, 313, 314, 315, 316, 317, 318, 319 },
        ["devotion"] = new[] { 320, 321, 322, 323, 324, 325, 326, 327 },
        ["toewyding"] = new[] { 328, 329, 330, 331, 332, 333, 334, 335 },
        
        // Majesty and power
        ["majesty"] = new[] { 336, 337, 338, 339, 340, 341, 342, 343 },
        ["mighty"] = new[] { 344, 345, 346, 347, 348, 349, 350, 351 },
        ["powerful"] = new[] { 352, 353, 354, 355, 356, 357, 358, 359 },
        ["kragtig"] = new[] { 360, 361, 362, 363, 364, 365, 366, 367 },
        ["almighty"] = new[] { 368, 369, 370, 371, 372, 373, 374, 375 },
        ["alvermogende"] = new[] { 376, 377, 378, 379, 380, 381, 382, 383 },
        
        // Glory and honor
        ["glory"] = new[] { 384, 385, 386, 387, 388, 389, 390, 391 },
        ["honor"] = new[] { 392, 393, 394, 395, 396, 397, 398, 399 },
        ["eer"] = new[] { 400, 401, 402, 403, 404, 405, 406, 407 },
        ["splendor"] = new[] { 408, 409, 410, 411, 412, 413, 414, 415 },
        ["luister"] = new[] { 416, 417, 418, 419, 420, 421, 422, 423 },
        
        // Heaven and eternal
        ["heaven"] = new[] { 424, 425, 426, 427, 428, 429, 430, 431 },
        ["hemel"] = new[] { 432, 433, 434, 435, 436, 437, 438, 439 },
        ["eternal"] = new[] { 440, 441, 442, 443, 444, 445, 446, 447 },
        ["ewig"] = new[] { 448, 449, 450, 451, 452, 453, 454, 455 },
        
        // Music and singing
        ["sing"] = new[] { 456, 457, 458, 459, 460, 461, 462, 463 },
        ["song"] = new[] { 464, 465, 466, 467, 468, 469, 470, 471 },
        ["music"] = new[] { 472, 473, 474, 475, 476, 477, 478, 479 },
        ["sang"] = new[] { 480, 481, 482, 483, 484, 485, 486, 487 },
        ["melody"] = new[] { 488, 489, 490, 491, 492, 493, 494, 495 },
        ["melodie"] = new[] { 496, 497, 498, 499, 500, 501, 502, 503 },
        ["chorus"] = new[] { 504, 505, 506, 507, 508, 509, 510, 511 },
        ["refrein"] = new[] { 512, 513, 514, 515, 516, 517, 518, 519 },
        
        // Common words (lower priority)
        ["die"] = new[] { 520, 521, 522 },
        ["the"] = new[] { 523, 524, 525 },
        ["my"] = new[] { 526, 527, 528 },
        ["is"] = new[] { 529, 530, 531 },
        ["in"] = new[] { 532, 533, 534 },
        ["of"] = new[] { 535, 536, 537 },
        ["to"] = new[] { 538, 539, 540 },
        ["and"] = new[] { 541, 542, 543 },
        ["you"] = new[] { 544, 545, 546 },
        ["me"] = new[] { 547, 548, 549 },
        ["hy"] = new[] { 550, 551, 552 },
        ["he"] = new[] { 553, 554, 555 },
        ["vir"] = new[] { 556, 557, 558 },
        ["for"] = new[] { 559, 560, 561 },
        ["sy"] = new[] { 562, 563, 564 },
        ["his"] = new[] { 565, 566, 567 },
        ["ons"] = new[] { 568, 569, 570 },
        ["our"] = new[] { 571, 572, 573 },
        ["u"] = new[] { 574, 575, 576 },
        ["your"] = new[] { 577, 578, 579 },
        ["ek"] = new[] { 580, 581, 582 },
        ["i"] = new[] { 583, 584, 585 },
        ["jy"] = new[] { 586, 587, 588 },
        ["you"] = new[] { 589, 590, 591 },
        ["kom"] = new[] { 592, 593, 594 },
        ["come"] = new[] { 595, 596, 597 },
        ["wat"] = new[] { 598, 599, 600 },
        ["what"] = new[] { 601, 602, 603 },
        ["op"] = new[] { 604, 605, 606 },
        ["on"] = new[] { 607, 608, 609 },
        ["het"] = new[] { 610, 611, 612 },
        ["has"] = new[] { 613, 614, 615 },
        ["hom"] = new[] { 616, 617, 618 },
        ["him"] = new[] { 619, 620, 621 },
        ["dit"] = new[] { 622, 623, 624 },
        ["it"] = new[] { 625, 626, 627 },
        ["so"] = new[] { 628, 629, 630 },
        ["sal"] = new[] { 631, 632, 633 },
        ["will"] = new[] { 634, 635, 636 },
        ["met"] = new[] { 637, 638, 639 },
        ["with"] = new[] { 640, 641, 642 },
        ["aan"] = new[] { 643, 644, 645 },
        ["at"] = new[] { 646, 647, 648 },
        ["en"] = new[] { 649, 650, 651 },
        ["and"] = new[] { 652, 653, 654 },
        ["van"] = new[] { 655, 656, 657 },
        ["from"] = new[] { 658, 659, 660 },
        ["all"] = new[] { 661, 662, 663 },
        ["almal"] = new[] { 664, 665, 666 },
        ["here"] = new[] { 667, 668, 669 },
        ["hier"] = new[] { 670, 671, 672 },
        ["are"] = new[] { 673, 674, 675 },
        ["is"] = new[] { 676, 677, 678 },
        ["will"] = new[] { 679, 680, 681 },
        ["gaan"] = new[] { 682, 683, 684 },
        ["great"] = new[] { 685, 686, 687 },
        ["groot"] = new[] { 688, 689, 690 },
        ["wonderful"] = new[] { 691, 692, 693 },
        ["wonderlik"] = new[] { 694, 695, 696 },
        ["amazing"] = new[] { 697, 698, 699 },
        ["verbasend"] = new[] { 700, 701, 702 },
        ["beautiful"] = new[] { 703, 704, 705 },
        ["mooi"] = new[] { 706, 707, 708 },
        ["glorious"] = new[] { 709, 710, 711 },
        ["heerlik"] = new[] { 712, 713, 714 },
        ["majestic"] = new[] { 715, 716, 717 },
        ["majestueus"] = new[] { 718, 719, 720 },
        ["awesome"] = new[] { 721, 722, 723 },
        ["ontsaglik"] = new[] { 724, 725, 726 },
        ["creation"] = new[] { 727, 728, 729 },
        ["skepping"] = new[] { 730, 731, 732 },
        ["world"] = new[] { 733, 734, 735 },
        ["wêreld"] = new[] { 736, 737, 738 },
        ["deliverance"] = new[] { 739, 740, 741 },
        ["verlossing"] = new[] { 742, 743, 744 },
        ["rescue"] = new[] { 745, 746, 747 },
        ["redding"] = new[] { 748, 749, 750 },
        ["saving"] = new[] { 751, 752, 753 },
        ["bevryding"] = new[] { 754, 755, 756 },
        ["liberation"] = new[] { 757, 758, 759 },
        ["vryheid"] = new[] { 760, 761, 762 },
        ["hallelujah"] = new[] { 763, 764, 765 },
        ["amen"] = new[] { 766, 767, 768 },
        ["harmony"] = new[] { 769, 770, 771 },
        ["harmonie"] = new[] { 772, 773, 774 },
        ["adore"] = new[] { 775, 776, 777 },
        ["aanbid"] = new[] { 778, 779, 780 },
        ["compassion"] = new[] { 781, 782, 783 },
        ["medelye"] = new[] { 784, 785, 786 },
        ["charity"] = new[] { 787, 788, 789 },
        ["liefdadigheid"] = new[] { 790, 791, 792 },
        ["affection"] = new[] { 793, 794, 795 },
        ["liefde"] = new[] { 796, 797, 798 },
        ["favor"] = new[] { 799, 800, 801 },
        ["gunsteling"] = new[] { 802, 803, 804 },
        ["blessing"] = new[] { 805, 806, 807 },
        ["seën"] = new[] { 808, 809, 810 },
        ["forgiveness"] = new[] { 811, 812, 813 },
        ["vergifnis"] = new[] { 814, 815, 816 },
        ["pity"] = new[] { 817, 818, 819 },
        ["jammer"] = new[] { 820, 821, 822 },
        ["confidence"] = new[] { 823, 824, 825 },
        ["vertroue"] = new[] { 826, 827, 828 },
        ["assurance"] = new[] { 829, 830, 831 },
        ["versekering"] = new[] { 832, 833, 834 },
        ["rely"] = new[] { 835, 836, 837 },
        ["vertrou"] = new[] { 838, 839, 840 },
        ["expectation"] = new[] { 841, 842, 843 },
        ["verwagting"] = new[] { 844, 845, 846 },
        ["supplication"] = new[] { 847, 848, 849 },
        ["smeekbede"] = new[] { 850, 851, 852 },
        ["intercession"] = new[] { 853, 854, 855 },
        ["voorbidding"] = new[] { 856, 857, 858 },
        ["petition"] = new[] { 859, 860, 861 },
        ["versoek"] = new[] { 862, 863, 864 },
        ["strong"] = new[] { 865, 866, 867 },
        ["sterk"] = new[] { 868, 869, 870 },
        ["wonderful"] = new[] { 871, 872, 873 },
        ["wonderlik"] = new[] { 874, 875, 876 },
        ["amazing"] = new[] { 877, 878, 879 },
        ["verbasend"] = new[] { 880, 881, 882 },
        ["magnificent"] = new[] { 883, 884, 885 },
        ["pragtig"] = new[] { 886, 887, 888 },
        ["earth"] = new[] { 889, 890, 891 },
        ["aarde"] = new[] { 892, 893, 894 },
        ["universe"] = new[] { 895, 896, 897 },
        ["heelal"] = new[] { 898, 899, 900 },
        ["nature"] = new[] { 901, 902, 903 },
        ["natuur"] = new[] { 904, 905, 906 },
        ["paradise"] = new[] { 907, 908, 909 },
        ["paradys"] = new[] { 910, 911, 912 },
        ["divine"] = new[] { 913, 914, 915 },
        ["goddelik"] = new[] { 916, 917, 918 },
        ["celestial"] = new[] { 919, 920, 921 },
        ["hemels"] = new[] { 922, 923, 924 }
    };

    public VectorSearchService(QdrantSettings settings, ILogger<VectorSearchService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<List<ChorusSearchResult>> SearchSimilarAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeClientAsync();
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Searching for similar choruses with query: {Query}", query);

            // Generate RAG-optimized embedding for the query
            var queryEmbedding = await GenerateRagOptimizedEmbeddingAsync(query);
            cancellationToken.ThrowIfCancellationRequested();

            // Search in Qdrant
            var searchResponse = await _client!.SearchAsync(
                collectionName: _settings.CollectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)maxResults,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Qdrant search returned {Count} results", searchResponse.Count);

            var results = new List<ChorusSearchResult>();
            foreach (var point in searchResponse)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (point.Payload != null)
                {
                    var payloadDict = point.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    var result = new ChorusSearchResult
                    {
                        Id = point.Id.Uuid, // Get ID from PointId, not payload
                        Name = GetPayloadValue(payloadDict, "name") ?? "",
                        ChorusText = GetPayloadValue(payloadDict, "chorusText") ?? "",
                        Key = ParseIntSafely(GetPayloadValue(payloadDict, "key")),
                        Type = ParseIntSafely(GetPayloadValue(payloadDict, "type")),
                        TimeSignature = ParseIntSafely(GetPayloadValue(payloadDict, "timeSignature")),
                        Score = point.Score
                    };

                    _logger.LogDebug("Vector search result: ID={Id}, Name={Name}, Score={Score}", result.Id, result.Name, result.Score);

                    // Apply semantic similarity boost
                    var semanticScore = CalculateSemanticSimilarity(query, result.ChorusText, ExtractKeywordsFromQuery(query));
                    result.Score = (result.Score + semanticScore) / 2.0f;

                    results.Add(result);
                }
            }

            // Sort by score and take top results
            var sortedResults = results.OrderByDescending(r => r.Score).Take(maxResults).ToList();
            
            _logger.LogInformation("Returning {Count} sorted results for query: {Query}", sortedResults.Count, query);
            return sortedResults;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Vector search was cancelled for query: {Query}", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during vector search for query: {Query}", query);
            throw;
        }
    }
    
    private float CalculateSemanticSimilarity(string query, string content, List<string> keywords)
    {
        var score = 0.0f;
        
        // Check for keyword matches
        foreach (var keyword in keywords)
        {
            if (content.Contains(keyword))
            {
                score += 0.1f;
            }
        }
        
        // Check for exact phrase matches
        if (content.Contains(query))
        {
            score += 0.5f;
        }
        
        // Check for word overlap
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contentWords = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var overlap = queryWords.Count(qw => contentWords.Any(cw => cw.Contains(qw) || qw.Contains(cw)));
        score += (float)overlap / queryWords.Length * 0.3f;
        
        return Math.Min(score, 1.0f);
    }

    public async Task<List<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await GenerateRagOptimizedEmbeddingAsync(text);
    }

    private async Task<List<float>> GenerateRagOptimizedEmbeddingAsync(string text)
    {
        try
        {
            // Use the same RAG-optimized embedding generation as the console app
            var normalizedText = text.ToLowerInvariant().Trim();
            
            // Initialize embedding vector
            var embedding = new float[VECTOR_DIMENSION];
            
            // Extract words from text
            var words = Regex.Split(normalizedText, @"\W+").Where(w => w.Length > 0).ToList();
            
            // RAG-optimized word-based features with enhanced semantic weighting
            foreach (var word in words)
            {
                if (WordPositions.ContainsKey(word))
                {
                    var positions = WordPositions[word];
                    var semanticWeight = GetSemanticWeight(word);
                    
                    foreach (var position in positions)
                    {
                        if (position < VECTOR_DIMENSION)
                        {
                            embedding[position] += semanticWeight;
                        }
                    }
                }
            }
            
            // Enhanced frequency-based features for RAG
            var wordFreq = words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
            foreach (var word in wordFreq.Take(30)) // Increased to top 30 for better RAG
            {
                var hash = Math.Abs(ComputeWordHash(word.Key));
                var positions = new int[5]; // Increased positions for better coverage
                for (int i = 0; i < 5; i++)
                {
                    positions[i] = (hash + i * 100) % VECTOR_DIMENSION;
                }
                foreach (var position in positions)
                {
                    if (position >= 0 && position < VECTOR_DIMENSION)
                    {
                        embedding[position] += (float)word.Value / words.Count * GetSemanticWeight(word.Key);
                    }
                }
            }
            
            // RAG-optimized text length features
            var lengthFeature = Math.Min(text.Length / 300.0f, 1.0f); // Adjusted for typical chorus length
            var lengthPositions = new int[8]; // More positions for length
            for (int i = 0; i < 8; i++)
            {
                lengthPositions[i] = Math.Abs((text.Length + i * 50) % VECTOR_DIMENSION);
            }
            foreach (var position in lengthPositions)
            {
                if (position >= 0 && position < VECTOR_DIMENSION)
                {
                    embedding[position] += lengthFeature;
                }
            }
            
            // Enhanced language detection for RAG
            var afrikaansWords = words.Count(w => IsAfrikaansWord(w));
            var englishWords = words.Count(w => IsEnglishWord(w));
            var languageRatio = afrikaansWords > 0 ? (float)afrikaansWords / (afrikaansWords + englishWords) : 0.5f;
            
            var languagePositions = new int[10]; // More positions for language
            for (int i = 0; i < 10; i++)
            {
                languagePositions[i] = Math.Abs((1200 + i * 30) % VECTOR_DIMENSION);
            }
            foreach (var position in languagePositions)
            {
                if (position >= 0 && position < VECTOR_DIMENSION)
                {
                    embedding[position] += languageRatio;
                }
            }
            
            // RAG-specific features: question words and context indicators
            var questionWords = new[] { "what", "when", "where", "who", "why", "how", "wat", "wanneer", "waar", "wie", "waarom", "hoe" };
            var contextWords = new[] { "chorus", "song", "hymn", "refrein", "lied", "psalm", "praise", "worship", "prys", "aanbidding" };
            
            var questionScore = words.Count(w => questionWords.Contains(w)) / (float)Math.Max(words.Count, 1);
            var contextScore = words.Count(w => contextWords.Contains(w)) / (float)Math.Max(words.Count, 1);
            
            // Add question and context features
            for (int i = 0; i < 5; i++)
            {
                var questionPos = Math.Abs((1400 + i * 20) % VECTOR_DIMENSION);
                var contextPos = Math.Abs((1450 + i * 20) % VECTOR_DIMENSION);
                
                if (questionPos < VECTOR_DIMENSION)
                {
                    embedding[questionPos] += questionScore;
                }
                if (contextPos < VECTOR_DIMENSION)
                {
                    embedding[contextPos] += contextScore;
                }
            }
            
            // Normalize the embedding
            var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < VECTOR_DIMENSION; i++)
                {
                    embedding[i] /= magnitude;
                }
            }
            
            return embedding.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG-optimized embedding for text: {Text}", text);
            throw;
        }
    }

    private List<string> ExtractKeywordsFromQuery(string query)
    {
        var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Where(w => w.Length > 2).ToList();
    }

    private float GetSemanticWeight(string word)
    {
        // Higher weights for religious/spiritual terms for better RAG
        var highPriorityWords = new HashSet<string> { "jesus", "christ", "god", "lord", "heer", "holy", "spirit", "praise", "worship", "prys", "aanbid", "salvation", "redemption", "verlossing", "verlosser", "grace", "mercy", "genade", "barmhartigheid", "faith", "trust", "geloof", "vertroue", "love", "liefde" };
        var mediumPriorityWords = new HashSet<string> { "prayer", "pray", "gebed", "smeking", "majesty", "mighty", "powerful", "kragtig", "glory", "honor", "eer", "heaven", "hemel", "eternal", "ewig", "sing", "song", "music", "sang", "melody", "melodie", "chorus", "refrein" };
        
        if (highPriorityWords.Contains(word)) return 2.0f;
        if (mediumPriorityWords.Contains(word)) return 1.5f;
        return 1.0f;
    }

    private bool IsAfrikaansWord(string word)
    {
        var afrikaansWords = new HashSet<string> { "die", "my", "en", "van", "ons", "u", "ek", "hy", "vir", "sy", "so", "sal", "met", "aan", "wat", "op", "het", "hom", "dit", "jy", "kom", "heer", "prys", "liefde", "genade", "geloof", "gebed", "verheerlik", "aanbid", "barmhartigheid", "gunst", "vertroue", "hoop", "aanbidding", "smeking", "kragtig", "alvermogende", "heerlik", "majestueus", "ontsaglik", "skepping", "wêreld", "verlossing", "redding", "bevryding", "vryheid", "harmonie", "aanbid", "medelye", "liefdadigheid", "liefde", "gunsteling", "seën", "vergifnis", "jammer", "vertroue", "versekering", "vertrou", "verwagting", "smeekbede", "voorbidding", "versoek", "sterk", "wonderlik", "verbasend", "pragtig", "aarde", "heelal", "natuur", "paradys", "goddelik", "hemels", "refrein", "lied", "psalm", "aanbidding" };
        return afrikaansWords.Contains(word);
    }

    private bool IsEnglishWord(string word)
    {
        var englishWords = new HashSet<string> { "the", "my", "is", "in", "of", "jesus", "to", "i", "and", "you", "me", "god", "all", "your", "his", "he", "lord", "for", "on", "here", "love", "praise", "him", "will", "are", "sing", "it", "christ", "savior", "redeemer", "almighty", "worship", "glory", "honor", "grace", "mercy", "kindness", "faith", "trust", "hope", "belief", "prayer", "pray", "great", "mighty", "powerful", "awesome", "creation", "world", "heaven", "salvation", "redemption", "deliverance", "music", "song", "hallelujah", "amen", "chorus", "melody", "harmony", "glorify", "exalt", "magnify", "adore", "majesty", "splendor", "magnificence", "compassion", "charity", "affection", "favor", "blessing", "forgiveness", "pity", "confidence", "assurance", "rely", "expectation", "supplication", "intercession", "petition", "strong", "wonderful", "amazing", "magnificent", "earth", "universe", "nature", "paradise", "eternal", "divine", "celestial", "rescue", "saving", "liberation", "hymn" };
        return englishWords.Contains(word);
    }

    private int ComputeTextHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToInt32(hashBytes, 0);
    }

    private int ComputeWordHash(string word)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(word));
        return BitConverter.ToInt32(hashBytes, 0);
    }

    public async Task<List<ChorusSearchResult>> GetAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeClientAsync();
            
            // Get all points from the collection
            var scrollResponse = await _client!.ScrollAsync(
                collectionName: _settings.CollectionName,
                limit: 1000, // Get all choruses (assuming less than 1000)
                cancellationToken: cancellationToken
            );

            var results = new List<ChorusSearchResult>();
            
            foreach (var point in scrollResponse.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var payloadDict = point.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                var chorusResult = new ChorusSearchResult
                {
                    Id = point.Id.Uuid,
                    Score = 1.0f, // Default score for all choruses
                    Name = GetPayloadValue(payloadDict, "name") ?? "",
                    ChorusText = GetPayloadValue(payloadDict, "chorusText") ?? "",
                    Key = ParseIntSafely(GetPayloadValue(payloadDict, "key")),
                    Type = ParseIntSafely(GetPayloadValue(payloadDict, "type")),
                    TimeSignature = ParseIntSafely(GetPayloadValue(payloadDict, "timeSignature")),
                    CreatedAt = ParseDateTimeSafely(GetPayloadValue(payloadDict, "createdAt"))
                };
                
                results.Add(chorusResult);
            }

            _logger.LogInformation("Retrieved {Count} choruses from vector database", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all choruses from vector database");
            throw;
        }
    }

    private async Task InitializeClientAsync()
    {
        if (_client == null)
        {
            _client = new QdrantClient(_settings.Host, _settings.Port + 1); // Use gRPC port
            _logger.LogDebug("Initialized Qdrant client for collection: {CollectionName}", _settings.CollectionName);
        }
    }

    private static string? GetPayloadValue(Dictionary<string, Value> payload, string key)
    {
        return payload.TryGetValue(key, out var value) ? value.StringValue : null;
    }

    private static int ParseIntSafely(string? value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    private static DateTime ParseDateTimeSafely(string? value)
    {
        return DateTime.TryParse(value, out var result) ? result : DateTime.MinValue;
    }
} 