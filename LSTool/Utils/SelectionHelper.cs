using Autodesk.Revit.UI.Selection;

namespace LSTool.Utils
{
    public static class SelectionHelper
    {
        /// <summary>
        /// Picks a single element from the document with optional category and custom filter.
        /// </summary>
        /// <param name="sel">The selection object.</param>
        /// <param name="doc">The Revit document.</param>
        /// <param name="bic">Optional built-in category filter.</param>
        /// <param name="elementFilter">Optional custom element filter.</param>
        /// <param name="statusPrompt">Prompt text shown during selection.</param>
        /// <returns>The picked element or null if not selected.</returns>
        public static Element PickElement(
            this Selection sel,
            Document doc,
            BuiltInCategory? bic = null,
            Func<Element, bool> elementFilter = null,
            string statusPrompt = "Pick an Element...")
        {
            var combinedFilter = CreateElementFilter(bic, elementFilter);

            Reference reference = sel.PickObject(
                ObjectType.Element,
                new ElementSelectionFilter(combinedFilter),
                statusPrompt);

            return doc.GetElement(reference) ?? null;
        }

        public static List<Element> PickElements(
            this Selection sel,
            Document doc,
            BuiltInCategory? bic = null,
            Func<Element, bool> elementFilter = null,
            string statusPrompt = "Pick an Element...")
        {
            var combinedFilter = CreateElementFilter(bic, elementFilter);

            var references = sel.PickObjects(
                ObjectType.Element,
                new ElementSelectionFilter(combinedFilter),
                statusPrompt);

            return references.Select(x => doc.GetElement(x)).ToList();
        }

        /// <summary>
        /// Picks a reference to a single element with optional category and custom filter.
        /// </summary>
        /// <param name="sel">The selection object.</param>
        /// <param name="bic">Optional built-in category filter.</param>
        /// <param name="elementFilter">Optional custom element filter.</param>
        /// <param name="statusPrompt">Prompt text shown during selection.</param>
        /// <returns>The reference to the picked element.</returns>
        public static Reference PickElementReference(
            this Selection sel,
            BuiltInCategory? bic = null,
            Func<Element, bool> elementFilter = null,
            string statusPrompt = "Pick an Element...")
        {
            var combinedFilter = CreateElementFilter(bic, elementFilter);

            return sel.PickObject(
                ObjectType.Element,
                new ElementSelectionFilter(combinedFilter),
                statusPrompt);
        }

        /// <summary>
        /// Picks a reference to an element that matches one of the given categories and optional custom filter.
        /// </summary>
        /// <param name="sel">The selection object.</param>
        /// <param name="categories">List of built-in categories to filter.</param>
        /// <param name="elementFilter">Optional custom element filter.</param>
        /// <param name="statusPrompt">Prompt text shown during selection.</param>
        /// <returns>The reference to the picked element.</returns>
        public static Reference PickElementReferenceFilters(
            this Selection sel,
            List<BuiltInCategory?> categories = null,
            Func<Element, bool> elementFilter = null,
            string statusPrompt = "Pick an Element...")
        {
            Func<Element, bool> filter = e =>
            {
                if (e?.Category == null) return false;

                var cat = e.Category.ToBuiltinCategory();
                bool inCategoryList = categories?.Any(c => c == cat) ?? true;
                return inCategoryList && (elementFilter?.Invoke(e) ?? true);
            };

            return sel.PickObject(ObjectType.Element, new ElementSelectionFilter(filter), statusPrompt);
        }

        /// <summary>
        /// Selects multiple elements within a rectangular area using optional category and custom filter.
        /// </summary>
        /// <param name="sel">The selection object.</param>
        /// <param name="bic">Optional built-in category filter.</param>
        /// <param name="elementFilter">Optional custom element filter.</param>
        /// <param name="statusPrompt">Prompt text shown during selection.</param>
        /// <returns>List of selected elements.</returns>
        public static IList<Element> SelectElementByRectangle(
            this Selection sel,
            BuiltInCategory? bic = null,
            Func<Element, bool> elementFilter = null,
            string statusPrompt = "Pick Elements by Rectangle...")
        {
            var combinedFilter = CreateElementFilter(bic, elementFilter);

            return sel.PickElementsByRectangle(new ElementSelectionFilter(combinedFilter), statusPrompt);
        }

        /// <summary>
        /// Creates a combined element filter based on category and custom predicate.
        /// </summary>
        /// <param name="bic">Optional built-in category.</param>
        /// <param name="elementFilter">Optional custom filter predicate.</param>
        /// <returns>Combined filter function.</returns>
        private static Func<Element, bool> CreateElementFilter(BuiltInCategory? bic, Func<Element, bool> elementFilter)
        {
            return element =>
            {
                if (element?.Category == null) return false;

                var elementCategory = element.Category.ToBuiltinCategory();
                bool categoryMatch = !bic.HasValue || elementCategory == bic.Value;
                bool customFilterMatch = elementFilter?.Invoke(element) ?? true;

                return categoryMatch && customFilterMatch;
            };
        }
    }

    /// <summary>
    /// Custom ISelectionFilter implementation using a predicate for filtering elements.
    /// </summary>
    internal class ElementSelectionFilter : ISelectionFilter
    {
        private readonly Func<Element, bool> _elementPredicate;

        /// <summary>
        /// Initializes the filter with a custom predicate.
        /// </summary>
        /// <param name="elementPredicate">Predicate to filter elements.</param>
        public ElementSelectionFilter(Func<Element, bool> elementPredicate)
        {
            _elementPredicate = elementPredicate ?? (e => true);
        }

        public bool AllowElement(Element elem) => _elementPredicate(elem);

        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}
