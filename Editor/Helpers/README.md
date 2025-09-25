# Helpers

## ImGuiFileDialog

Displays an Open/Save file dialog.

https://github.com/japajoe/ImGuiFileDialog

## ImGuiMessageDialog

Displays a simple message dialog.

### How to use it?

* First declare a variable:
```csharp
private ImGuiMessageDialog.Configuration? _messageDialogConfiguration;
```

* In your ImGui loop, perform:
```csharp
        var buttonPressed = ImGuiMessageDialog.Run(_messageDialogConfiguration);

        if (buttonPressed != null)
            _messageDialogConfiguration = null;

        if (buttonPressed != null)
        {
            Logger.Trace($"{buttonPressed.Id} has been pressed");

            buttonPressed.OnPressed?.Invoke(buttonPressed);
        }
```

* Finally trigger the display of the message dialog using:
```csharp
_messageDialogConfiguration = new("Current file has not been saved",
    "Are you sure you want to continue?",
    [
        new ImGuiMessageDialog.ButtonConfiguration(ImGuiMessageDialog.ButtonId.Yes, "Yes, I'm sure",
            _ => PerformTheActionYouNeed(),
            System.Drawing.Color.Red),
        new ImGuiMessageDialog.ButtonConfiguration(ImGuiMessageDialog.ButtonId.No, "No, I changed my mind")
    ]);
```
