using Autodesk.Revit.DB.DirectContext3D;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.UI;
using Color = Autodesk.Revit.DB.Color;
using View = Autodesk.Revit.DB.View;

namespace LSTool.Utils
{
    public abstract class ElementVisualizationServer : IDirectContext3DServer
    {
        private readonly Guid _guid;
        public UIDocument _uiDocument;
        public Document _document;
        private List<RenderingBufferStorage> _faceBuffers;
        public List<RenderingBufferStorage> _edgeBuffers { get; set; }
        private double _transparency;
        private double _scale;
        private Color _faceColor;
        private Color _edgeColor;
        private bool _drawFace;
        private bool _drawEdge;
        private object _renderLock;
        private bool _hasEffectsUpdates;
        private bool _hasGeometryUpdates;
        protected ElementVisualizationServer(UIDocument uiDocument)
        {
            _guid = Guid.NewGuid();
            _uiDocument = uiDocument;
            _document = _uiDocument.Document;
            _faceBuffers = new List<RenderingBufferStorage>();
            _edgeBuffers = new List<RenderingBufferStorage>();
            _drawEdge = true;
            _hasEffectsUpdates = false;
            _hasGeometryUpdates = true;
            _renderLock = new object();
        }
        public virtual Guid GetServerId() => _guid;
        public virtual string GetVendorId() => "ElementVisualizationServer";
        public virtual string GetName() => "ElementVisualizationServer";
        public virtual string GetDescription() => "ElementVisualizationServer";
        public virtual ExternalServiceId GetServiceId()
            => ExternalServices.BuiltInExternalServices.DirectContext3DService;
        public virtual string GetApplicationId() => string.Empty;
        public virtual string GetSourceId() => string.Empty;
        public virtual bool UsesHandles() => false;
        public virtual bool CanExecute(View dBView) => true;
        public virtual bool UseInTransparentPass(View dBView) => false;
        public virtual Outline GetBoundingBox(View dBView) => null;
        public virtual void RenderScene(View dBView, DisplayStyle displayStyle)
        {
            lock (_renderLock)
            {
                try
                {
                    _hasGeometryUpdates = true;
                    if (_hasGeometryUpdates)
                    {
                        MapGeometryBuffer();
                        _hasGeometryUpdates = false;
                    }
                    if (_hasEffectsUpdates)
                    {
                        UpdateEffects();
                        _hasEffectsUpdates = false;
                    }

                    if (_drawFace)
                    {
                        var isTransparentPass = DrawContext.IsTransparentPass();
                        if (isTransparentPass && _transparency > 0 || !isTransparentPass && _transparency == 0)
                        {
                            foreach (var buffer in _faceBuffers)
                            {
                                if (buffer.VertexBuffer == null)
                                    continue;
                                DrawContext.FlushBuffer(
                                    buffer.VertexBuffer,
                                    buffer.VertexBufferCount,
                                    buffer.IndexBuffer,
                                    buffer.IndexBufferCount,
                                    buffer.VertexFormat,
                                    buffer.EffectInstance, PrimitiveType.TriangleList, 0,
                                    buffer.PrimitiveCount);
                            }
                        }
                    }

                    if (_drawEdge)
                    {
                        foreach (var buffer in _edgeBuffers)
                        {
                            DrawContext.FlushBuffer(buffer.VertexBuffer,
                                buffer.VertexBufferCount,
                                buffer.IndexBuffer,
                                buffer.IndexBufferCount,
                                buffer.VertexFormat,
                                buffer.EffectInstance, PrimitiveType.LineList, 0,
                                buffer.PrimitiveCount);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public virtual void MapGeometryBuffer() { }
        private void UpdateEffects()
        {
            foreach (var buffer in _faceBuffers)
            {
                if (buffer.EffectInstance == null)
                {
                    buffer.EffectInstance = new EffectInstance(buffer.FormatBits);
                }

                buffer.EffectInstance.SetColor(_faceColor);
                buffer.EffectInstance.SetTransparency(_transparency);
            }

            foreach (var buffer in _edgeBuffers)
            {
                if (buffer.EffectInstance == null)
                {
                    buffer.EffectInstance = new EffectInstance(buffer.FormatBits);
                }

                buffer.EffectInstance.SetColor(_edgeColor);
            }
        }
        public void UpdateFaceColor(Color value)
        {
            var uiDocument = _uiDocument;
            if (uiDocument is null) return;

            lock (_renderLock)
            {
                _faceColor = value;
                _hasEffectsUpdates = true;

                uiDocument.UpdateAllOpenViews();
            }
        }
        public void UpdateEdgeColor(Color value)
        {
            var uiDocument = _uiDocument;
            if (uiDocument is null) return;

            lock (_renderLock)
            {
                _edgeColor = value;
                _hasEffectsUpdates = true;

                uiDocument.UpdateAllOpenViews();
            }
        }
        public void UpdateTransparency(double value)
        {
            var uiDocument = _uiDocument;
            if (uiDocument is null) return;

            lock (_renderLock)
            {
                _transparency = value;
                _hasEffectsUpdates = true;

                uiDocument.UpdateAllOpenViews();
            }
        }
        public void UpdateScale(double value)
        {
            var uiDocument = _uiDocument;
            if (uiDocument is null) return;

            _scale = value;

            lock (_renderLock)
            {
                _hasGeometryUpdates = true;
                _hasEffectsUpdates = true;
                _faceBuffers.Clear();
                _edgeBuffers.Clear();

                uiDocument.UpdateAllOpenViews();
            }
        }
        public void UpdateFaceVisibility(bool value)
        {
            var uiDocument = _uiDocument;
            if (uiDocument is null) return;

            lock (_renderLock)
            {
                _drawFace = value;

                uiDocument.UpdateAllOpenViews();
            }
        }
        public void UpdateEdgeVisibility(bool value)
        {
            var uiDocument = _uiDocument;
            if (uiDocument is null) return;

            lock (_renderLock)
            {
                _drawEdge = value;

                uiDocument.UpdateAllOpenViews();
            }
        }
        public virtual void Register()
        {
            try
            {
                if (this.IsRegisterServer())
                    return;
                var directContextService = (MultiServerService)ExternalServiceRegistry
                    .GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
                var serverIds = directContextService.GetActiveServerIds();
                directContextService.AddServer(this);
                serverIds.Add(GetServerId());
                directContextService.SetActiveServers(serverIds);

                _uiDocument?.UpdateAllOpenViews();
            }
            catch (Exception ex)
            {
                IO.ShowInfo(nameof(CurveVisualizationServer));
                throw new Exception(ex.Message, ex);
            }
        }
        public virtual void UnRegister()
        {
            try
            {
                if (!IsRegisterServer())
                    return;
                var directContextService = (MultiServerService)ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
                directContextService.RemoveServer(GetServerId());
                _uiDocument?.UpdateAllOpenViews();
            }
            catch (Exception ex)
            {
                IO.ShowInfo(nameof(ElementVisualizationServer));
                throw new Exception(ex.Message, ex);
            }
        }
        public virtual void UnAllRegister()
        {
            try
            {
                var directContextService = (MultiServerService)ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
                foreach (var item in directContextService.GetActiveServerIds())
                {
                    directContextService.RemoveServer(item);
                }
                _uiDocument?.UpdateAllOpenViews();
            }
            catch (Exception ex)
            {
                IO.ShowInfo(nameof(ElementVisualizationServer));
                throw new Exception(ex.Message, ex);
            }
        }
        public virtual bool IsRegisterServer()
        {
            try
            {
                var externalDrawerServiceId = ExternalServices.BuiltInExternalServices.DirectContext3DService;
                if (!(ExternalServiceRegistry.GetService(externalDrawerServiceId) is MultiServerService
                    externalDrawerService))
                {
                    return false;
                }

                return externalDrawerService.GetRegisteredServerIds().Contains(GetServerId());
            }
            catch (Exception ex)
            {
                IO.ShowInfo(nameof(ElementVisualizationServer));
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
