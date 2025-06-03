using System.Collections.Generic;

namespace WordleSolver.Strategies;

/// <summary>
/// Example solver that simply iterates through a fixed list of words.
/// Students will replace this with a smarter algorithm.
/// </summary>
public sealed class SlowStudentSolver25 : IWordleSolverStrategy
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
            var guess = previousResult.Word;
            var statuses = previousResult.LetterStatuses;

            for (int i = 0; i < 5; i++)
            {
                var status = statuses[i];
                char c = guess[i];

                if (status == LetterStatus.Correct)
                {
                    foreach (string word in _remainingWords.ToList())
                    {
                        if (word[i] != c)
                        {
                            _remainingWords.Remove(word);
                        }
                    }
                }
                else if (status == LetterStatus.Misplaced)
                {
                    foreach (string word in _remainingWords.ToList())
                    {
                        if (word[i] == c || !word.Contains(c))
                        {
                            _remainingWords.Remove(word);
                        }
                    }
                }
                
                else if (status == LetterStatus.Unused)
                {
                    // Only remove words containing the letter if it doesn't appear as Correct or Misplaced elsewhere
                    bool letterElsewhere = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (j != i && previousResult.Word[j] == c &&
                            (previousResult.LetterStatuses[j] == LetterStatus.Correct ||
                             previousResult.LetterStatuses[j] == LetterStatus.Misplaced))
                        {
                            letterElsewhere = true;
                            break;
                        }
                    }
                    if (!letterElsewhere)
                    {
                        foreach (string word in _remainingWords.ToList())
                        {
                            if (word.Contains(c))
                            {
                                _remainingWords.Remove(word);
                            }
                        }
                    }
                    else
                    {
                        foreach (string word in _remainingWords.ToList())
                        {
                            if (word[i] == c)
                            {
                                _remainingWords.Remove(word);
                            }
                        }
                    }
                }
            }
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

        for (int i = 0; i < totalWords; i++)
        {
            string g = _remainingWords[i];

            // Build the partition‐counts for guess g
            var partitions = new Dictionary<string, int>(capacity: totalWords);
            for (int j = 0; j < totalWords; j++)
            {
                string w = _remainingWords[j];
                string pattern = FeedBack(g, w);

                if (partitions.ContainsKey(pattern))
                    partitions[pattern]++;
                else
                    partitions[pattern] = 1;
            }

            // Compute Shannon entropy over those partition sizes
            double entropy = 0.0;
            foreach (var count in partitions.Values)
            {
                double p = (double)count / totalWords;
                // If p == 0, skip (though count > 0 by construction)
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
    
    public string FeedBack(string guess, string answer)
    {
        var answerCharCounts = new Dictionary<char, int>();
        foreach (char c in answer)
        {
            if (answerCharCounts.ContainsKey(c))
                answerCharCounts[c]++;
            else
                answerCharCounts[c] = 1;
        }

        char[] statusChars = new char[5] { '0', '0', '0', '0', '0' };

        for (int i = 0; i < 5; i++)
        {
            if (guess[i] == answer[i])
            {
                statusChars[i] = '2';
                answerCharCounts[guess[i]]--;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (statusChars[i] == '2')
                continue;

            char g = guess[i];
            if (answerCharCounts.TryGetValue(g, out int remaining) && remaining > 0)
            {
                statusChars[i] = '1';
                answerCharCounts[g]--;
            }
        }

        return new string(statusChars);
    }
}