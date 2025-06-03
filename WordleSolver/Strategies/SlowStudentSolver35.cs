
using System.Collections.Generic;

namespace WordleSolver.Strategies;

/// <summary>
/// Example solver that simply iterates through a fixed list of words.
/// Students will replace this with a smarter algorithm.
/// </summary>
public sealed class SlowStudentSolver35 : IWordleSolverStrategy
{
	/// <summary>Absolute or relative path of the word-list file.</summary>
	private static readonly string WordListPath = Path.Combine("data", "wordle.txt");

	/// <summary>In-memory dictionary of valid five-letter words.</summary>
	private static readonly List<string> WordList = LoadWordList();

    /// <summary>
    /// Remaining words that can be chosen
    /// </summary>
    private List<string> _remainingWords = new();
    
    // TODO: ADD your own private variables that you might need

    /// <summary>
    /// Loads the dictionary from disk, filtering to distinct five-letter lowercase words.
    /// </summary>
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

    /// <inheritdoc/>
    public void Reset()
    {
		// TODO: What should happen when a new game starts?

		// If using SLOW student strategy, we just reset the current index
		// to the first word to start the next guessing sequence
        _remainingWords = [..WordList];  // Set _remainingWords to a copy of the full word list
    }

    /// <summary>
    /// Determines the next word to guess given feedback from the previous guess.
    /// </summary>
    /// <param name="previousResult">
    /// The <see cref="GuessResult"/> returned by the game engine for the last guess
    /// (or <see cref="GuessResult.Default"/> if this is the first turn).
    /// </param>
    /// <returns>A five-letter lowercase word.</returns>
    public string PickNextGuess(GuessResult previousResult)
    {
        // Analyze previousResult and remove any words from
        // _remainingWords that aren't possible

        if (!previousResult.IsValid)
            throw new InvalidOperationException("PickNextGuess shouldn't be called if previous result isn't valid");

        // Check if first guess
        if (previousResult.Guesses.Count == 0)
        {
            
        }
        else
        {
            string guess = previousResult.Word;
            LetterStatus[] statuses = previousResult.LetterStatuses;

            _remainingWords = _remainingWords
                .Where(w => IsConsistent(w, guess, statuses))
                .ToList();
        }

        // Utilize the remaining words to choose the next guess
        string choice = ChooseBestRemainingWord(previousResult);
        _remainingWords.Remove(choice);

        return choice;
    }

    /// <summary>
    /// Pick the best of the remaining words according to some heuristic.
    /// For example, you might want to choose the word that has the most
    /// common letters found in the remaining words list
    /// </summary>
    /// <param name="previousResult"></param>
    /// <returns></returns>
    public string ChooseBestRemainingWord(GuessResult previousResult)
    {
        if (_remainingWords.Count == 0)
            throw new InvalidOperationException("No remaining words to choose from");

        string bestGuess = _remainingWords[0];
        double bestEntropy = Double.NegativeInfinity;
        int totalWords = _remainingWords.Count;

        int[] counts = new int[243];
        List<int> usedBuckets = new List<int>(10); // track which codes we’ve touched
        
        for (int i = 0; i < WordList.Count; i++)
        {
            string g = WordList[i];

            // Build the partition‐counts for guess g
            
            Array.Clear(counts, 0, 243);
            usedBuckets.Clear();

            for (int j = 0; j < totalWords; j++)
            {
                int code = GetPatternCodeFaster(g, _remainingWords[j]);
                if (counts[code] == 0)
                    usedBuckets.Add(code);
                counts[code]++;
            }

// Now compute entropy from only the usedBuckets:
            double entropy = 0.0;
            foreach (int code in usedBuckets)
            {
                int c = counts[code];
                double p = (double)c / totalWords;
                entropy += -p * Math.Log(p, 2);
            }

            // Keep the guess with the highest entropy
            if (entropy > bestEntropy)
            {
                bestEntropy = entropy;
                bestGuess = g;
            }
        }

        return bestGuess;
    }
    
    private int GetPatternCode(string guess, string answer)
    {
        // Build a small array of 5 ints: 0=Unused, 1=Misplaced, 2=Correct
        int[] status = new int[5];
        int[] freq = new int[26];

        // Count answer freq
        for (int i = 0; i < 5; i++)
            freq[answer[i] - 'a']++;

        // First pass: mark Greens
        for (int i = 0; i < 5; i++)
        {
            if (guess[i] == answer[i])
            {
                status[i] = 2;           // “2” = correct
                freq[guess[i] - 'a']--;
            }
        }

        // Second pass: mark Yellows vs. Grays
        for (int i = 0; i < 5; i++)
        {
            if (status[i] == 2) 
                continue;

            int idx = guess[i] - 'a';
            if (freq[idx] > 0)
            {
                status[i] = 1;           // “1” = misplaced
                freq[idx]--;
            }
            else
            {
                status[i] = 0;           // “0” = unused
            }
        }

        // Now encode base‐3: status[0]*3^4 + status[1]*3^3 + ... + status[4]*3^0
        int code = 0;
        for (int i = 0; i < 5; i++)
        {
            code = code * 3 + status[i];
        }
        return code;
    }
    public int GetPatternCodeFaster(string guess, string answer)
    {
        // freq[0] = count of 'a', freq[1] = count of 'b', etc.
        int[] freq = new int[26];
        for (int i = 0; i < 5; i++)
            freq[answer[i] - 'a']++;

        int[] status = new int[5]; // 0=gray, 1=yellow, 2=green

        // First pass: mark greens
        for (int i = 0; i < 5; i++)
            if (guess[i] == answer[i])
            {
                status[i] = 2;
                freq[guess[i] - 'a']--;
            }

        // Second pass: yellows vs. grays
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

        // Pack into base-3 integer
        int code = 0;
        for (int i = 0; i < 5; i++)
            code = code * 3 + status[i];
        return code;
    }
    private bool IsConsistent(string word, string guess, LetterStatus[] statuses)
    {
        // 1) Count leftover letters from `word` (for handling Misplaced vs. Unused)
        int[] freq = new int[26];
        for (int i = 0; i < 5; i++)
            freq[word[i] - 'a']++;

        // 2) First pass: verify all Greens (Correct) and decrement freq
        for (int i = 0; i < 5; i++)
        {
            if (statuses[i] == LetterStatus.Correct)
            {
                if (guess[i] != word[i]) 
                    return false;
                freq[word[i] - 'a']--;
            }
        }

        // 3) Second pass: check Misplaced vs. Unused
        for (int i = 0; i < 5; i++)
        {
            char g = guess[i];
            int idx = g - 'a';

            if (statuses[i] == LetterStatus.Misplaced)
            {
                // letter must appear elsewhere in `word` and NOT in this position
                if (word[i] == g || freq[idx] == 0)
                    return false;
                freq[idx]--;
            }
            else if (statuses[i] == LetterStatus.Unused)
            {
                // “Unused” means: either that letter doesn’t appear at all in any unmatched slot,
                // or it’s already been “accounted for” by a Correct/Misplaced elsewhere.
                if (freq[idx] > 0) 
                    return false;
            }
            // if Correct, we already checked in first pass
        }

        return true;
    }
}