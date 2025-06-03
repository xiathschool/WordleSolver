
using System.Collections.Generic;

namespace WordleSolver.Strategies;

/// <summary>
/// Example solver that simply iterates through a fixed list of words.
/// Students will replace this with a smarter algorithm.
/// </summary>
public sealed class SlowStudentSolver1 : IWordleSolverStrategy
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
            // TODO: Pick the best starting word from wordle.txt 
            // BE CAREFUL that the first word you pick is in that wordle.txt list or your
            // program won't work. Regular Wordle allows users to guess any five-letter
            // word from a much larger dictionary, but we restrict it to the words that
            // can actually be chosen by WordleService to make it easier on you.
            string firstWord = "abyss"; 

            // Filter _remainingWords to remove any words that don't match the first word
            _remainingWords.Remove(firstWord);

            return firstWord;  
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
        
        var random = new Random();
        int index = random.Next(_remainingWords.Count);
        return _remainingWords[index];
    }
}