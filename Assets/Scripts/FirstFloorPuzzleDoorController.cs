using UnityEngine;

public class FirstFloorPuzzleDoorController : MonoBehaviour
{
    [SerializeField] private PuzzleAssemblyManager eyePuzzleManager;
    [SerializeField] private PuzzleAssemblyManager eaglePuzzleManager;
    [SerializeField] private Transform doorHinge;
    [SerializeField] private float openAngle = -110f;
    [SerializeField] private float openSpeed = 90f;
    [SerializeField] private bool openOnPlayForDebug = false;
    [SerializeField] private bool debugDoorState = true;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private bool _initialized;
    private bool _lastEyeConfigured;
    private bool _lastEagleConfigured;
    private bool _lastEyeCompleted;
    private bool _lastEagleCompleted;
    private bool _lastShouldOpen;
    private string _lastEyeSummary = string.Empty;
    private string _lastEagleSummary = string.Empty;

    private void Start()
    {
        if (doorHinge == null)
        {
            doorHinge = transform;
        }

        _closedRotation = doorHinge.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(openAngle, 0f, 0f);
        _initialized = true;

        bool eyeConfigured = SafeConfigured(eyePuzzleManager);
        bool eagleConfigured = SafeConfigured(eaglePuzzleManager);
        bool eyeCompleted = SafeCompleted(eyePuzzleManager);
        bool eagleCompleted = SafeCompleted(eaglePuzzleManager);
        string eyeSummary = GetSummary(eyePuzzleManager);
        string eagleSummary = GetSummary(eaglePuzzleManager);
        bool shouldOpen = openOnPlayForDebug || (eyeConfigured && eagleConfigured && eyeCompleted && eagleCompleted);
        RememberState(eyeConfigured, eagleConfigured, eyeCompleted, eagleCompleted, shouldOpen, eyeSummary, eagleSummary);
        LogDebug($"Start closed={doorHinge.localEulerAngles} openOnPlayForDebug={openOnPlayForDebug} eyeConfigured={eyeConfigured} eagleConfigured={eagleConfigured} eyeCompleted={eyeCompleted} eagleCompleted={eagleCompleted} eyeSummary={eyeSummary} eagleSummary={eagleSummary} shouldOpen={shouldOpen}");
    }

    private void Update()
    {
        if (!_initialized || doorHinge == null)
        {
            return;
        }

        bool eyeConfigured = SafeConfigured(eyePuzzleManager);
        bool eagleConfigured = SafeConfigured(eaglePuzzleManager);
        bool eyeCompleted = SafeCompleted(eyePuzzleManager);
        bool eagleCompleted = SafeCompleted(eaglePuzzleManager);
        string eyeSummary = GetSummary(eyePuzzleManager);
        string eagleSummary = GetSummary(eaglePuzzleManager);
        bool shouldOpen = openOnPlayForDebug || (eyeConfigured && eagleConfigured && eyeCompleted && eagleCompleted);

        if (StateChanged(eyeConfigured, eagleConfigured, eyeCompleted, eagleCompleted, shouldOpen, eyeSummary, eagleSummary))
        {
            LogDebug($"Update shouldOpen={shouldOpen} eyeCompleted={eyeCompleted} eagleCompleted={eagleCompleted} eyeConfigured={eyeConfigured} eagleConfigured={eagleConfigured} eyeSummary={eyeSummary} eagleSummary={eagleSummary} currentEuler={doorHinge.localEulerAngles}");
            RememberState(eyeConfigured, eagleConfigured, eyeCompleted, eagleCompleted, shouldOpen, eyeSummary, eagleSummary);
        }

        if (eyeCompleted)
        {
            eyePuzzleManager.SetPlacedOutlines(true);
        }

        if (eagleCompleted)
        {
            eaglePuzzleManager.SetPlacedOutlines(true);
        }

        if (!shouldOpen)
        {
            return;
        }

        doorHinge.localRotation = Quaternion.RotateTowards(
            doorHinge.localRotation,
            _openRotation,
            openSpeed * Time.deltaTime);
    }

    private bool StateChanged(bool eyeConfigured, bool eagleConfigured, bool eyeCompleted, bool eagleCompleted, bool shouldOpen, string eyeSummary, string eagleSummary)
    {
        return eyeConfigured != _lastEyeConfigured ||
            eagleConfigured != _lastEagleConfigured ||
            eyeCompleted != _lastEyeCompleted ||
            eagleCompleted != _lastEagleCompleted ||
            shouldOpen != _lastShouldOpen ||
            eyeSummary != _lastEyeSummary ||
            eagleSummary != _lastEagleSummary;
    }

    private void RememberState(bool eyeConfigured, bool eagleConfigured, bool eyeCompleted, bool eagleCompleted, bool shouldOpen, string eyeSummary, string eagleSummary)
    {
        _lastEyeConfigured = eyeConfigured;
        _lastEagleConfigured = eagleConfigured;
        _lastEyeCompleted = eyeCompleted;
        _lastEagleCompleted = eagleCompleted;
        _lastShouldOpen = shouldOpen;
        _lastEyeSummary = eyeSummary;
        _lastEagleSummary = eagleSummary;
    }

    private void LogDebug(string message)
    {
        if (!debugDoorState)
        {
            return;
        }

        Debug.Log($"[FirstFloorPuzzleDoorController] {message}", this);
    }

    private static bool SafeCompleted(PuzzleAssemblyManager manager)
    {
        return manager != null && manager.AreAllPiecesPlaced();
    }

    private static bool SafeConfigured(PuzzleAssemblyManager manager)
    {
        return manager != null && manager.HasConfiguredPieces;
    }

    private static string GetSummary(PuzzleAssemblyManager manager)
    {
        return manager == null ? "null" : manager.GetCompletionSummary();
    }
}
