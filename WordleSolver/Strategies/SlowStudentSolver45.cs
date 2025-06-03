/*
 * The code uses filtering as normal. However, when picking potential answers, it takes the list of possible answers and for each potential answer, assumes that if it was the correct answer, what pattern it would have. it then creates a histogram of this. Using this metric, we can now compare which guesses are better than others. This is done with the Shannon Entropy formula, which in essence, is a weighted sum of each pattern and the number of potential solutions in the pattern. A higher entropy means that selecting this guess would give the most "exploration" of possible answers. This is done repeatedly until a correct answer is selected. There are many other small optimizations to speed up overhead, but they do not change the overall stratedy
 * 
 */
using System.Collections.Generic;

namespace WordleSolver.Strategies;

/// <summary>
/// Example solver that simply iterates through a fixed list of words.
/// Students will replace this with a smarter algorithm.
/// </summary>
public sealed class SlowStudentSolver45 : IWordleSolverStrategy
{
    private static readonly string WordListPath = Path.Combine("data", "wordle.txt");
    private static readonly List<string> WordList = LoadWordList();
    private readonly Dictionary<string,int> _wordToIndex;
    private readonly int[,] _feedbackTable; // [guessIndex, answerIndex] → patternCode

    private List<string> _remainingWords;
    
    private static List<string> LoadWordList()
    {
        if (!File.Exists(WordListPath))
            throw new FileNotFoundException($"Word list not found at path: {WordListPath}");

        return File.ReadAllLines(WordListPath)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length == 5)
            .Distinct()
            .ToList();
    }

    public SlowStudentSolver45()
    {
        // Build a map word→index
        _wordToIndex = new Dictionary<string, int>(capacity: WordList.Count);
        for (int i = 0; i < WordList.Count; i++)
            _wordToIndex[WordList[i]] = i;

        // Precompute all pair feedback codes
        int N = WordList.Count;
        _feedbackTable = new int[N, N];
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
                _feedbackTable[i, j] = GetPatternCode(WordList[i], WordList[j]);
    }

    public void Reset()
    {
        _remainingWords = new List<string>(WordList);
    }

    public string PickNextGuess(GuessResult previousResult)
    {
        if (!_remainingWords.Any())
            throw new InvalidOperationException("No remaining words");

        // 1) Filter _remainingWords based on previous feedback
        if (previousResult.Guesses.Count > 0)
        {
            string guess = previousResult.Word;
            var statuses = previousResult.LetterStatuses;

            var filtered = new List<string>(_remainingWords.Count);
            foreach (var w in _remainingWords)
            {
                if (IsConsistent(w, guess, statuses))
                    filtered.Add(w);
            }
            _remainingWords = filtered;
        }
        // 2) If we have shrunk to 2 or fewer possible answers, pick directly from _remainingWords.
        if (_remainingWords.Count <= 2)
        {
            // Choose the first candidate (you could randomize if you prefer).
            string narrowChoice = _remainingWords[0];
            _remainingWords.RemoveAt(0);
            return narrowChoice;
        }

        // 2) Choose best next guess by entropy
        string bestGuess = _remainingWords[0];
        double bestEntropy = Double.NegativeInfinity;
        int total = _remainingWords.Count;

        // Pre‐allocate a counts array per guess
        int[] counts = new int[243];
        var usedBuckets = new List<int>(16);

        for (int i = 0; i < total; i++)
        {
            string g = _remainingWords[i];
            int gi = _wordToIndex[g];

            // Clear counts and usedBuckets
            Array.Clear(counts, 0, 243);
            usedBuckets.Clear();

            // Build partition counts
            for (int j = 0; j < total; j++)
            {
                int wi = _wordToIndex[_remainingWords[j]];
                int code = _feedbackTable[gi, wi];

                if (counts[code] == 0)
                    usedBuckets.Add(code);
                counts[code]++;
            }

            // Compute entropy
            double entropy = 0.0;
            double invTotal = 1.0 / total;
            foreach (int code in usedBuckets)
            {
                double p = counts[code] * invTotal;
                entropy += -p * Math.Log2(p);
            }

            if (entropy > bestEntropy)
            {
                bestEntropy = entropy;
                bestGuess = g;
            }
        }

        // 3) Remove bestGuess from _remainingWords so we don’t guess it again
        _remainingWords.Remove(bestGuess);
        return bestGuess;
    }

    private bool IsConsistent(string word, string guess, LetterStatus[] statuses)
    {
        int[] freq = new int[26];
        for (int i = 0; i < 5; i++)
            freq[word[i] - 'a']++;

        // First pass: Confirm Greens
        for (int i = 0; i < 5; i++)
        {
            if (statuses[i] == LetterStatus.Correct)
            {
                if (guess[i] != word[i]) 
                    return false;
                freq[word[i] - 'a']--;
            }
        }

        // Second pass: Misplaced vs. Unused
        for (int i = 0; i < 5; i++)
        {
            char gChar = guess[i];
            int idx = gChar - 'a';

            if (statuses[i] == LetterStatus.Misplaced)
            {
                if (word[i] == gChar || freq[idx] == 0)
                    return false;
                freq[idx]--;
            }
            else if (statuses[i] == LetterStatus.Unused)
            {
                if (freq[idx] > 0)
                    return false;
            }
        }

        return true;
    }

    private int GetPatternCode(string guess, string answer)
    {
        // identical to the earlier base-3 approach
        int[] freq = new int[26];
        for (int i = 0; i < 5; i++)
            freq[answer[i] - 'a']++;

        int[] status = new int[5];
        for (int i = 0; i < 5; i++)
        {
            if (guess[i] == answer[i])
            {
                status[i] = 2;
                freq[guess[i] - 'a']--;
            }
        }
        for (int i = 0; i < 5; i++)
        {
            if (status[i] == 2) continue;
            int idx = guess[i] - 'a';
            if (freq[idx] > 0)
            {
                status[i] = 1;
                freq[idx]--;
            }
            else
            {
                status[i] = 0;
            }
        }

        int code = 0;
        for (int i = 0; i < 5; i++)
            code = code * 3 + status[i];
        return code;
    }
}