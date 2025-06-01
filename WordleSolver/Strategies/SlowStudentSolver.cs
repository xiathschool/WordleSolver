
using System.Collections.Generic;

namespace WordleSolver.Strategies;

/// <summary>
/// Example solver that simply iterates through a fixed list of words.
/// Students will replace this with a smarter algorithm.
/// </summary>
public sealed class SlowStudentSolver : IWordleSolverStrategy
{
	/// <summary>Absolute or relative path of the word-list file.</summary>
	private static readonly string WordListPath = Path.Combine("data", "wordle.txt");

	/// <summary>In-memory dictionary of valid five-letter words.</summary>
	private static readonly List<string> WordList = LoadWordList();
	
    private int _currentIndex;

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
		_currentIndex = 0; 
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
        if (_currentIndex >= WordList.Count)
            _currentIndex = 0;

        return WordList[_currentIndex++];
    }
}