using System.Text;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics
{
    internal static class RebarErrorMessageBuilder
    {
        public static string Build(
            Exception exception,
            string operation,
            string? diagnosticLogPath = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));
            if (string.IsNullOrWhiteSpace(operation))
                operation = "The operation";

#if DEBUG
            return BuildDebugMessage(
                exception,
                operation,
                diagnosticLogPath);
#else
            return BuildReleaseMessage(
                exception,
                operation,
                diagnosticLogPath);
#endif
        }

        private static string BuildDebugMessage(
            Exception exception,
            string operation,
            string? diagnosticLogPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[DEBUG] {operation} failed.");
            builder.AppendLine();
            builder.AppendLine("Exception chain:");

            var index = 0;
            for (var current = exception;
                 current != null;
                 current = current.InnerException)
            {
                builder.AppendLine(
                    $"[{index}] {current.GetType().FullName}: "
                    + current.Message);
                index++;
            }

            builder.AppendLine();
            builder.AppendLine("Full exception and stack trace:");
            builder.AppendLine(exception.ToString());
            AppendDiagnosticLog(builder, diagnosticLogPath);
            return builder.ToString().TrimEnd();
        }

        private static string BuildReleaseMessage(
            Exception exception,
            string operation,
            string? diagnosticLogPath)
        {
            var messages = GetExceptionMessages(exception);
            var guidance = ResolveUserGuidance(messages);
            var builder = new StringBuilder();
            builder.AppendLine($"{operation} could not be completed.");
            builder.AppendLine();
            builder.AppendLine("What happened:");
            builder.AppendLine(guidance.Problem);
            builder.AppendLine();
            builder.AppendLine("What to do:");
            builder.AppendLine(guidance.Action);
            AppendDiagnosticLog(builder, diagnosticLogPath);
            return builder.ToString().TrimEnd();
        }

        private static IReadOnlyList<string> GetExceptionMessages(
            Exception exception)
        {
            var messages = new List<string>();
            for (var current = exception;
                 current != null;
                 current = current.InnerException)
            {
                var message = current.Message?.Trim();
                if (!string.IsNullOrWhiteSpace(message)
                    && !messages.Contains(
                        message,
                        StringComparer.OrdinalIgnoreCase))
                {
                    messages.Add(message);
                }
            }
            return messages;
        }

        private static UserGuidance ResolveUserGuidance(
            IReadOnlyList<string> messages)
        {
            var combined = string.Join(" ", messages);
            if (Contains(combined, "multiple active bar types"))
            {
                var source = FindMessage(
                    messages,
                    "multiple active bar types");
                var layer = GetLayerLabel(source);
                var diameters = GetParenthesizedValue(source);
                var diameterText = string.IsNullOrWhiteSpace(diameters)
                    ? string.Empty
                    : $" ({diameters})";
                return new UserGuidance(
                    $"{layer} uses different bar diameters between "
                    + $"the Start, Middle and End sections{diameterText}.",
                    "Set Layer 1 to the same diameter in all three "
                    + "sections, then run the command again. Layers 2 "
                    + "and 3 may use different diameters.");
            }

            if (ContainsAny(
                    combined,
                    "inconsistent nominal/model diameters",
                    "remains inconsistent after synchronization"))
            {
                return new UserGuidance(
                    "One or more Rebar Bar Types use different nominal and "
                    + "modeled diameters, so Revit geometry would not match "
                    + "the selected bar size.",
                    "Open Rebar Diameter Settings and save once to "
                    + "synchronize the configured types, then run the beam "
                    + "reinforcement command again.");
            }

            if (ContainsAny(
                    combined,
                    "active section without a bar type",
                    "bar types have not been initialized",
                    "rebar type name is required",
                    "was not found in the active document",
                    "bar type could not be resolved",
                    "diameter could not be resolved"))
            {
                return new UserGuidance(
                    "A required rebar diameter is not selected or its "
                    + "Rebar Bar Type is unavailable in this project.",
                    "Select a diameter for every active section and make "
                    + "sure the matching Rebar Bar Type is loaded in the "
                    + "current Revit model.");
            }

            if (ContainsAny(
                    combined,
                    "select at least one beam",
                    "structural framing",
                    "beam grouping",
                    "beam index",
                    "plan direction",
                    "beam run",
                    "not connected",
                    "not parallel"))
            {
                return new UserGuidance(
                    "The selected beams cannot be treated as one "
                    + "continuous beam run.",
                    "Select connected structural framing elements in "
                    + "their physical order. Do not mix unrelated or "
                    + "non-parallel beams in the same run.");
            }

            if (Contains(combined, "InvalidRebarStandardHMin"))
            {
                return new UserGuidance(
                    "The General Setting hMin value is missing or invalid.",
                    "Open General Setting and enter a positive hMin value. "
                    + "hMin is measured in bar diameters (for example, "
                    + "10 means 10D), then run the command again.");
            }

            if (Contains(
                    combined,
                    "IndependentAnchorInsufficientBentAnchorAvailability"))
            {
                return new UserGuidance(
                    "Even the configured hMin bent tail does not fit inside "
                    + "the cover-reduced beam depth.",
                    "Reduce hMin only if the revised detail is approved, or "
                    + "revise the beam/rebar design before running the "
                    + "command again.");
            }

            if (Contains(combined, "stirrup"))
            {
                return new UserGuidance(
                    "The stirrup layout cannot be created from the "
                    + "current settings or beam geometry.",
                    "Check the stirrup diameter, spacing, shape, cover "
                    + "and the Start/Middle/End section settings, then "
                    + "run the command again.");
            }

            if (ContainsAny(
                    combined,
                    "shared parameter",
                    "parameterbindings",
                    "required parameter"))
            {
                return new UserGuidance(
                    "The required reinforcement parameters could not be "
                    + "prepared or written in this project.",
                    "Check that the document is editable and that the "
                    + "required shared-parameter file and permissions "
                    + "are available.");
            }

            if (ContainsAny(
                    combined,
                    "rehost",
                    "temporary host",
                    "target host",
                    "unexpected host"))
            {
                return new UserGuidance(
                    "Revit could not assign one or more created bars to "
                    + "the intended beam host.",
                    "Check that the selected beams are valid structural "
                    + "framing elements. If the error remains, send the "
                    + "diagnostic log to support.");
            }

            if (ContainsAny(
                    combined,
                    "main-bar",
                    "main bar",
                    "geometry planning",
                    "transition",
                    "lane planner",
                    "centerline"))
            {
                return new UserGuidance(
                    "The main-bar geometry cannot be generated safely "
                    + "for the selected beams and section settings.",
                    "Check bar quantities, diameters, beam dimensions, "
                    + "cover and the Start/Middle/End transitions, then "
                    + "run the command again.");
            }

            if (Contains(combined, "assembly"))
            {
                return new UserGuidance(
                    "Revit could not create the reinforcement assembly.",
                    "Check that all created reinforcement is valid and "
                    + "can belong to an assembly, then run the command "
                    + "again.");
            }

            var rootMessage = messages.LastOrDefault();
            return new UserGuidance(
                ToSingleLine(rootMessage),
                "Review the selected beams and reinforcement settings, "
                + "then run the command again. If the error remains, "
                + "send the diagnostic log to support.");
        }

        private static bool Contains(string value, string text)
            => value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool ContainsAny(
            string value,
            params string[] texts)
            => texts.Any(text => Contains(value, text));

        private static string FindMessage(
            IEnumerable<string> messages,
            string text)
            => messages.FirstOrDefault(message => Contains(message, text))
               ?? string.Empty;

        private static string GetLayerLabel(string message)
        {
            if (Contains(message, "top level 1"))
                return "Top reinforcement - Layer 1";
            if (Contains(message, "bottom level 1"))
                return "Bottom reinforcement - Layer 1";
            return "Layer 1 reinforcement";
        }

        private static string GetParenthesizedValue(string message)
        {
            var start = message.IndexOf('(');
            var end = start >= 0
                ? message.IndexOf(')', start + 1)
                : -1;
            return start >= 0 && end > start
                ? message.Substring(start + 1, end - start - 1).Trim()
                : string.Empty;
        }

        private static string ToSingleLine(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "An unexpected error occurred while processing "
                       + "the selected beams.";

            var singleLine = string.Join(
                " ",
                message
                    .Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Trim())
                    .Where(part => part.Length > 0));
            const int maximumLength = 500;
            return singleLine.Length <= maximumLength
                ? singleLine
                : singleLine.Substring(0, maximumLength) + "...";
        }

        private static void AppendDiagnosticLog(
            StringBuilder builder,
            string? diagnosticLogPath)
        {
            if (string.IsNullOrWhiteSpace(diagnosticLogPath))
                return;

            builder.AppendLine();
            builder.AppendLine("Diagnostic log:");
            builder.AppendLine(diagnosticLogPath);
        }

        private sealed class UserGuidance
        {
            public UserGuidance(string problem, string action)
            {
                Problem = problem;
                Action = action;
            }

            public string Problem { get; }

            public string Action { get; }
        }
    }
}
