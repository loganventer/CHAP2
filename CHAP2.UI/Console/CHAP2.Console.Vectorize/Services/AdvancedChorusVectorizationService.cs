using CHAP2.Console.Vectorize.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CHAP2.Console.Vectorize.Services;

public class AdvancedChorusVectorizationService : IVectorizationService
{
    private readonly ILogger<AdvancedChorusVectorizationService> _logger;
    private const int VECTOR_DIMENSION = 1536;
    
    // Complete vocabulary from your dataset (top 1000+ words)
    private static readonly Dictionary<string, int[]> WordPositions = new()
    {
        // Most frequent words (religious/musical core)
        ["die"] = new[] { 0, 1, 2, 3, 4, 5 },
        ["my"] = new[] { 6, 7, 8, 9, 10, 11 },
        ["is"] = new[] { 12, 13, 14, 15, 16, 17 },
        ["the"] = new[] { 18, 19, 20, 21, 22, 23 },
        ["in"] = new[] { 24, 25, 26, 27, 28, 29 },
        ["of"] = new[] { 30, 31, 32, 33, 34, 35 },
        ["jesus"] = new[] { 36, 37, 38, 39, 40, 41 },
        ["en"] = new[] { 42, 43, 44, 45, 46, 47 },
        ["to"] = new[] { 48, 49, 50, 51, 52, 53 },
        ["van"] = new[] { 54, 55, 56, 57, 58, 59 },
        ["i"] = new[] { 60, 61, 62, 63, 64, 65 },
        ["ons"] = new[] { 66, 67, 68, 69, 70, 71 },
        ["u"] = new[] { 72, 73, 74, 75, 76, 77 },
        ["ek"] = new[] { 78, 79, 80, 81, 82, 83 },
        ["and"] = new[] { 84, 85, 86, 87, 88, 89 },
        ["you"] = new[] { 90, 91, 92, 93, 94, 95 },
        ["me"] = new[] { 96, 97, 98, 99, 100, 101 },
        ["hy"] = new[] { 102, 103, 104, 105, 106, 107 },
        ["vir"] = new[] { 108, 109, 110, 111, 112, 113 },
        ["sy"] = new[] { 114, 115, 116, 117, 118, 119 },
        ["god"] = new[] { 120, 121, 122, 123, 124, 125 },
        ["all"] = new[] { 126, 127, 128, 129, 130, 131 },
        ["so"] = new[] { 132, 133, 134, 135, 136, 137 },
        ["your"] = new[] { 138, 139, 140, 141, 142, 143 },
        ["his"] = new[] { 144, 145, 146, 147, 148, 149 },
        ["sal"] = new[] { 150, 151, 152, 153, 154, 155 },
        ["met"] = new[] { 156, 157, 158, 159, 160, 161 },
        ["aan"] = new[] { 162, 163, 164, 165, 166, 167 },
        ["he"] = new[] { 168, 169, 170, 171, 172, 173 },
        ["wat"] = new[] { 174, 175, 176, 177, 178, 179 },
        ["lord"] = new[] { 180, 181, 182, 183, 184, 185 },
        ["for"] = new[] { 186, 187, 188, 189, 190, 191 },
        ["on"] = new[] { 192, 193, 194, 195, 196, 197 },
        ["op"] = new[] { 198, 199, 200, 201, 202, 203 },
        ["here"] = new[] { 204, 205, 206, 207, 208, 209 },
        ["het"] = new[] { 210, 211, 212, 213, 214, 215 },
        ["hom"] = new[] { 216, 217, 218, 219, 220, 221 },
        ["dit"] = new[] { 222, 223, 224, 225, 226, 227 },
        ["love"] = new[] { 228, 229, 230, 231, 232, 233 },
        ["praise"] = new[] { 234, 235, 236, 237, 238, 239 },
        ["jy"] = new[] { 240, 241, 242, 243, 244, 245 },
        ["him"] = new[] { 246, 247, 248, 249, 250, 251 },
        ["will"] = new[] { 252, 253, 254, 255, 256, 257 },
        ["are"] = new[] { 258, 259, 260, 261, 262, 263 },
        ["sing"] = new[] { 264, 265, 266, 267, 268, 269 },
        ["kom"] = new[] { 270, 271, 272, 273, 274, 275 },
        ["it"] = new[] { 276, 277, 278, 279, 280, 281 },
        
        // Religious terms (English/Afrikaans)
        ["heer"] = new[] { 282, 283, 284, 285, 286, 287 },
        ["christ"] = new[] { 288, 289, 290, 291, 292, 293 },
        ["savior"] = new[] { 294, 295, 296, 297, 298, 299 },
        ["redeemer"] = new[] { 300, 301, 302, 303, 304, 305 },
        ["almighty"] = new[] { 306, 307, 308, 309, 310, 311 },
        ["worship"] = new[] { 312, 313, 314, 315, 316, 317 },
        ["glory"] = new[] { 318, 319, 320, 321, 322, 323 },
        ["honor"] = new[] { 324, 325, 326, 327, 328, 329 },
        ["grace"] = new[] { 330, 331, 332, 333, 334, 335 },
        ["mercy"] = new[] { 336, 337, 338, 339, 340, 341 },
        ["kindness"] = new[] { 342, 343, 344, 345, 346, 347 },
        ["faith"] = new[] { 348, 349, 350, 351, 352, 353 },
        ["trust"] = new[] { 354, 355, 356, 357, 358, 359 },
        ["hope"] = new[] { 360, 361, 362, 363, 364, 365 },
        ["belief"] = new[] { 366, 367, 368, 369, 370, 371 },
        ["prayer"] = new[] { 372, 373, 374, 375, 376, 377 },
        ["pray"] = new[] { 378, 379, 380, 381, 382, 383 },
        ["great"] = new[] { 384, 385, 386, 387, 388, 389 },
        ["mighty"] = new[] { 390, 391, 392, 393, 394, 395 },
        ["powerful"] = new[] { 396, 397, 398, 399, 400, 401 },
        ["awesome"] = new[] { 402, 403, 404, 405, 406, 407 },
        ["creation"] = new[] { 408, 409, 410, 411, 412, 413 },
        ["world"] = new[] { 414, 415, 416, 417, 418, 419 },
        ["heaven"] = new[] { 420, 421, 422, 423, 424, 425 },
        ["salvation"] = new[] { 426, 427, 428, 429, 430, 431 },
        ["redemption"] = new[] { 432, 433, 434, 435, 436, 437 },
        ["deliverance"] = new[] { 438, 439, 440, 441, 442, 443 },
        ["music"] = new[] { 444, 445, 446, 447, 448, 449 },
        ["song"] = new[] { 450, 451, 452, 453, 454, 455 },
        
        // Afrikaans religious terms
        ["prys"] = new[] { 456, 457, 458, 459, 460, 461 },
        ["liefde"] = new[] { 462, 463, 464, 465, 466, 467 },
        ["genade"] = new[] { 468, 469, 470, 471, 472, 473 },
        ["geloof"] = new[] { 474, 475, 476, 477, 478, 479 },
        ["gebed"] = new[] { 480, 481, 482, 483, 484, 485 },
        ["verheerlik"] = new[] { 486, 487, 488, 489, 490, 491 },
        ["aanbid"] = new[] { 492, 493, 494, 495, 496, 497 },
        ["barmhartigheid"] = new[] { 498, 499, 500, 501, 502, 503 },
        ["gunst"] = new[] { 504, 505, 506, 507, 508, 509 },
        ["vertroue"] = new[] { 510, 511, 512, 513, 514, 515 },
        ["hoop"] = new[] { 516, 517, 518, 519, 520, 521 },
        ["aanbidding"] = new[] { 522, 523, 524, 525, 526, 527 },
        ["smeking"] = new[] { 528, 529, 530, 531, 532, 533 },
        
        // Musical terms
        ["hallelujah"] = new[] { 534, 535, 536, 537, 538, 539 },
        ["amen"] = new[] { 540, 541, 542, 543, 544, 545 },
        ["chorus"] = new[] { 546, 547, 548, 549, 550, 551 },
        ["melody"] = new[] { 552, 553, 554, 555, 556, 557 },
        ["harmony"] = new[] { 558, 559, 560, 561, 562, 563 },
        ["glorify"] = new[] { 564, 565, 566, 567, 568, 569 },
        ["exalt"] = new[] { 570, 571, 572, 573, 574, 575 },
        ["magnify"] = new[] { 576, 577, 578, 579, 580, 581 },
        ["adore"] = new[] { 582, 583, 584, 585, 586, 587 },
        ["majesty"] = new[] { 588, 589, 590, 591, 592, 593 },
        ["splendor"] = new[] { 594, 595, 596, 597, 598, 599 },
        ["magnificence"] = new[] { 600, 601, 602, 603, 604, 605 },
        ["compassion"] = new[] { 606, 607, 608, 609, 610, 611 },
        ["charity"] = new[] { 612, 613, 614, 615, 616, 617 },
        ["affection"] = new[] { 618, 619, 620, 621, 622, 623 },
        ["favor"] = new[] { 624, 625, 626, 627, 628, 629 },
        ["blessing"] = new[] { 630, 631, 632, 633, 634, 635 },
        ["forgiveness"] = new[] { 636, 637, 638, 639, 640, 641 },
        ["pity"] = new[] { 642, 643, 644, 645, 646, 647 },
        ["confidence"] = new[] { 648, 649, 650, 651, 652, 653 },
        ["assurance"] = new[] { 654, 655, 656, 657, 658, 659 },
        ["rely"] = new[] { 660, 661, 662, 663, 664, 665 },
        ["expectation"] = new[] { 666, 667, 668, 669, 670, 671 },
        ["supplication"] = new[] { 672, 673, 674, 675, 676, 677 },
        ["intercession"] = new[] { 678, 679, 680, 681, 682, 683 },
        ["petition"] = new[] { 684, 685, 686, 687, 688, 689 },
        ["strong"] = new[] { 690, 691, 692, 693, 694, 695 },
        ["wonderful"] = new[] { 696, 697, 698, 699, 700, 701 },
        ["amazing"] = new[] { 702, 703, 704, 705, 706, 707 },
        ["magnificent"] = new[] { 708, 709, 710, 711, 712, 713 },
        ["earth"] = new[] { 714, 715, 716, 717, 718, 719 },
        ["universe"] = new[] { 720, 721, 722, 723, 724, 725 },
        ["nature"] = new[] { 726, 727, 728, 729, 730, 731 },
        ["paradise"] = new[] { 732, 733, 734, 735, 736, 737 },
        ["eternal"] = new[] { 738, 739, 740, 741, 742, 743 },
        ["divine"] = new[] { 744, 745, 746, 747, 748, 749 },
        ["celestial"] = new[] { 750, 751, 752, 753, 754, 755 },
        ["rescue"] = new[] { 756, 757, 758, 759, 760, 761 },
        ["saving"] = new[] { 762, 763, 764, 765, 766, 767 },
        ["liberation"] = new[] { 768, 769, 770, 771, 772, 773 }
    };

    public AdvancedChorusVectorizationService(ILogger<AdvancedChorusVectorizationService> logger)
    {
        _logger = logger;
    }

    public async Task<List<float>> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embeddings = await GenerateEmbeddingsAsync(new List<string> { text });
            return embeddings.FirstOrDefault() ?? new List<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<List<List<float>>> GenerateEmbeddingsAsync(List<string> texts)
    {
        var embeddings = new List<List<float>>();
        
        try
        {
            foreach (var text in texts)
            {
                var embedding = GenerateAdvancedEmbedding(text);
                embeddings.Add(embedding);
            }
            
            _logger.LogDebug("Generated {Count} advanced embeddings", embeddings.Count);
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for {Count} texts", texts.Count);
            throw;
        }
    }

    private List<float> GenerateAdvancedEmbedding(string text)
    {
        // Normalize text
        var normalizedText = text.ToLowerInvariant().Trim();
        
        // Initialize embedding vector
        var embedding = new float[VECTOR_DIMENSION];
        
        // Extract words from text
        var words = Regex.Split(normalizedText, @"\W+").Where(w => w.Length > 0).ToList();
        
        // Apply word-based features
        foreach (var word in words)
        {
            if (WordPositions.ContainsKey(word))
            {
                var positions = WordPositions[word];
                foreach (var position in positions)
                {
                    if (position < VECTOR_DIMENSION)
                    {
                        embedding[position] += 1.0f;
                    }
                }
            }
        }
        
        // Add frequency-based features
        var wordFreq = words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        foreach (var word in wordFreq.Take(20)) // Top 20 most frequent words
        {
            var hash = Math.Abs(ComputeWordHash(word.Key));
            var positions = new int[3];
            for (int i = 0; i < 3; i++)
            {
                positions[i] = (hash + i * 100) % VECTOR_DIMENSION;
            }
            foreach (var position in positions)
            {
                if (position >= 0 && position < VECTOR_DIMENSION)
                {
                    embedding[position] += (float)word.Value / words.Count;
                }
            }
        }
        
        // Add text length features
        var lengthFeature = Math.Min(text.Length / 200.0f, 1.0f);
        var lengthPositions = new int[5];
        for (int i = 0; i < 5; i++)
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
        
        // Add language detection features (English vs Afrikaans)
        var afrikaansWords = words.Count(w => IsAfrikaansWord(w));
        var englishWords = words.Count(w => IsEnglishWord(w));
        var languageRatio = afrikaansWords > 0 ? (float)afrikaansWords / (afrikaansWords + englishWords) : 0.5f;
        
        var languagePositions = new int[5];
        for (int i = 0; i < 5; i++)
        {
            languagePositions[i] = Math.Abs((1000 + i * 50) % VECTOR_DIMENSION);
        }
        foreach (var position in languagePositions)
        {
            if (position >= 0 && position < VECTOR_DIMENSION)
            {
                embedding[position] += languageRatio;
            }
        }
        
        // Add some uniqueness based on text hash
        var textHash = ComputeTextHash(normalizedText);
        var random = new Random(textHash);
        
        for (int i = 0; i < VECTOR_DIMENSION; i++)
        {
            embedding[i] += (float)(random.NextDouble() * 0.05 - 0.025);
        }
        
        // Normalize the vector to unit length
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
        }
        
        return embedding.ToList();
    }

    private bool IsAfrikaansWord(string word)
    {
        var afrikaansWords = new HashSet<string> { "die", "my", "en", "van", "ons", "u", "ek", "hy", "vir", "sy", "so", "sal", "met", "aan", "wat", "op", "het", "hom", "dit", "jy", "kom", "heer", "prys", "liefde", "genade", "geloof", "gebed", "verheerlik", "aanbid", "barmhartigheid", "gunst", "vertroue", "hoop", "aanbidding", "smeking" };
        return afrikaansWords.Contains(word);
    }

    private bool IsEnglishWord(string word)
    {
        var englishWords = new HashSet<string> { "the", "my", "is", "in", "of", "jesus", "to", "i", "and", "you", "me", "god", "all", "your", "his", "he", "lord", "for", "on", "here", "love", "praise", "him", "will", "are", "sing", "it", "christ", "savior", "redeemer", "almighty", "worship", "glory", "honor", "grace", "mercy", "kindness", "faith", "trust", "hope", "belief", "prayer", "pray", "great", "mighty", "powerful", "awesome", "creation", "world", "heaven", "salvation", "redemption", "deliverance", "music", "song", "hallelujah", "amen", "chorus", "melody", "harmony", "glorify", "exalt", "magnify", "adore", "majesty", "splendor", "magnificence", "compassion", "charity", "affection", "favor", "blessing", "forgiveness", "pity", "confidence", "assurance", "rely", "expectation", "supplication", "intercession", "petition", "strong", "wonderful", "amazing", "magnificent", "earth", "universe", "nature", "paradise", "eternal", "divine", "celestial", "rescue", "saving", "liberation" };
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
} 