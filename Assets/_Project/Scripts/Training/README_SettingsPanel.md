# SettingsPanel UIToolkit Implementation

## Overview
This implementation replaces the legacy SettingCanvas with a modern UIToolkit-based SettingsPanel for VR table tennis training sessions.

## Components

### SettingsPanelController.cs
The main controller script that handles UIToolkit functionality:
- **UI Element Binding**: Automatically finds and binds to UXML elements
- **Event Handling**: Manages slider changes, button clicks, and toggle states
- **VR Integration**: Controls VR controller interactor visibility
- **Session Management**: Integrates with TrainingSessionManager for data persistence

### SettingsPanel.uxml
The UI layout definition containing:
- **ShotsPerSessionSlider**: Integer slider (1-50) for shots per training session
- **ShotIntervalSlider**: Integer slider (5-30, representing 0.5-3.0 seconds) for shot intervals
- **RemoveBallToggle**: Boolean toggle for ball removal after paddle hit
- **StartButtons 1-17**: Training start buttons with difficulty level mapping

## Usage Instructions

### Setup
1. Add UIDocument component to a GameObject
2. Assign SettingsPanel.uxml as the source asset
3. Add SettingsPanelController script to the same GameObject
4. Configure VR interactor references in the inspector

### VR Interactor Setup
- **controllerInteractors**: Parent GameObject containing all VR interactors
- **rightControllerInteractor**: Right hand VR controller
- **leftControllerInteractor**: Left hand VR controller

### Button Mapping
- **Buttons 1-7**: Course difficulty levels (1-5, extras ignored)
- **Buttons 8-12**: Speed levels (1-7, mapped to buttons 8-14)
- **Buttons 13-17**: Spin levels (1-5, mapped to buttons 13-17)

### Slider Value Mapping
- **Shots Slider**: Direct integer mapping (1-50 shots)
- **Interval Slider**: Integer scale mapping (5-30 = 0.5-3.0 seconds)

## Integration Points

### TrainingSessionManager
The controller automatically finds and integrates with:
- `shotsPerSession`: Number of shots per training session
- `shotInterval`: Time interval between shots (in seconds)
- `removeBalLAfterPaddleHit`: Whether to remove ball after paddle collision
- `difficultySettings`: Course, speed, and spin difficulty levels

### Public Methods
- `ToggleUI()`: Show/hide the settings panel
- `SetUIVisible(bool visible)`: Control panel visibility
- `SetInteractorsVisibility(bool visible)`: Control VR interactor visibility
- `SetInteractorVisibilityForHand(bool isRightHand, bool visible)`: Control individual hand visibility
- `RefreshUIFromSessionManager()`: Update UI from current session settings
- `SetSessionManager(TrainingSessionManager manager)`: Set session manager reference

## Testing
Use the SettingsPanelTest script for validation:
- Context menu options for testing UI functionality
- Validates component integration
- Tests VR interactor controls
- Verifies settings synchronization

## Migration from SettingCanvas
The new implementation provides full feature parity with the original SettingCanvas:
1. **Input Handling**: Sliders and toggle work identically
2. **Button Functionality**: Start buttons trigger session start with difficulty settings
3. **VR Support**: Controller raycasting and interaction preserved
4. **Data Persistence**: Full integration with existing TrainingSessionManager
5. **UI Visibility**: Show/hide functionality maintained

## Notes
- The interval slider uses integer scaling (x10) to handle decimal values
- Button indices above valid difficulty ranges are clamped to maximum values
- Defensive programming ensures graceful handling of missing components
- Event cleanup is handled automatically by UIToolkit lifecycle