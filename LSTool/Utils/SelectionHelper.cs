using Autodesk.Revit.UI.Selection;

namespace LSTool.Utils
{
    public static class SelectionHelper
    {
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

        public static IList<Element> SelectElementByRectangle(
            this Selection sel,
            BuiltInCategory? bic = null,
            Func<Element, bool> elementFilter = null,
            string statusPrompt = "Pick Elements by Rectangle...")
        {
            var combinedFilter = CreateElementFilter(bic, elementFilter);

            return sel.PickElementsByRectangle(new ElementSelectionFilter(combinedFilter), statusPrompt);
        }

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

    internal class ElementSelectionFilter : ISelectionFilter
    {
        private readonly Func<Element, bool> _elementPredicate;

        public ElementSelectionFilter(Func<Element, bool> elementPredicate)
        {
            _elementPredicate = elementPredicate ?? (e => true);
        }

        public bool AllowElement(Element elem) => _elementPredicate(elem);

        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}
