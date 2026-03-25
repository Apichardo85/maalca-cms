using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace MaalCaCMS.Composers
{
    public class DocumentTypeComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Components().Append<DocumentTypeComponent>();
        }
    }

    public class DocumentTypeComponent : IAsyncComponent
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly PropertyEditorCollection _propertyEditors;
        private readonly IConfigurationEditorJsonSerializer _serializer;
        private readonly IContentService _contentService;

        public DocumentTypeComponent(
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            IShortStringHelper shortStringHelper,
            PropertyEditorCollection propertyEditors,
            IConfigurationEditorJsonSerializer serializer,
            IContentService contentService)
        {
            _contentTypeService = contentTypeService;
            _dataTypeService    = dataTypeService;
            _shortStringHelper  = shortStringHelper;
            _propertyEditors    = propertyEditors;
            _serializer         = serializer;
            _contentService     = contentService;
        }

        public async Task InitializeAsync(bool isInitializing, CancellationToken cancellationToken)
        {
            // Element types primero: el BlockList los necesita
            await CreateBlockElementTypesAsync();
            await CreateEditorialContentDocumentTypeAsync();
            await CreateEPUBViewerDocumentTypeAsync();
            await CreateAzureAISearchDataTypesAsync();

            // maalca-web Document Types
            await CreateEcosystemProjectDocumentTypeAsync();
            await CreateArticleDocumentTypeAsync();
            await CreateBookDocumentTypeAsync();
            await CreatePropertyDocumentTypeAsync();
            await CreateDoctorProfileDocumentTypeAsync();
            await CreateCiriSonicServiceDocumentTypeAsync();
            await CreateAffiliateLandingDocumentTypeAsync();
            await CreateTestimonialDocumentTypeAsync();

            // Content folders for organizing content
            await CreateContentFoldersAsync();
        }

        public Task TerminateAsync(bool isTerminating, CancellationToken cancellationToken) => Task.CompletedTask;

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<IDataType> DtAsync(string editorAlias)
        {
            var all = await _dataTypeService.GetAllAsync();
            return all.First(dt => dt.EditorAlias == editorAlias);
        }

        private async Task<IDataType?> DtByNameAsync(string name)
        {
            var all = await _dataTypeService.GetAllAsync();
            return all.FirstOrDefault(dt => dt.Name == name);
        }

        private async Task<PropertyType> PropAsync(string editorAlias, Action<PropertyType> configure)
        {
            var pt = new PropertyType(_shortStringHelper, await DtAsync(editorAlias));
            configure(pt);
            return pt;
        }

        private static PropertyType PropDt(IShortStringHelper shortStringHelper, IDataType dataType, Action<PropertyType> configure)
        {
            var pt = new PropertyType(shortStringHelper, dataType);
            configure(pt);
            return pt;
        }

        // ── Block Element Types ───────────────────────────────────────────────

        private async Task CreateBlockElementTypesAsync()
        {
            await EnsureTextBlockAsync();
            await EnsureImageBlockAsync();
            await EnsureQuoteBlockAsync();
            await EnsureFileBlockAsync();
        }

        private async Task EnsureTextBlockAsync()
        {
            if (_contentTypeService.Get("textBlock") != null) return;

            var el = new ContentType(_shortStringHelper, -1)
            {
                Alias       = "textBlock",
                Name        = "Text Block",
                Icon        = "icon-paragraph",
                IsElement   = true,
                Description = "Bloque de texto enriquecido"
            };
            el.AddPropertyGroup("content", "Content");
            el.AddPropertyType(
                await PropAsync("Umbraco.TinyMCE", p =>
                {
                    p.Alias     = "richText";
                    p.Name      = "Rich Text";
                    p.Mandatory = true;
                }), "content");
            await _contentTypeService.CreateAsync(el, Constants.Security.SuperUserKey);
        }

        private async Task EnsureImageBlockAsync()
        {
            if (_contentTypeService.Get("imageBlock") != null) return;

            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();

            var el = new ContentType(_shortStringHelper, -1)
            {
                Alias       = "imageBlock",
                Name        = "Image Block",
                Icon        = "icon-picture",
                IsElement   = true,
                Description = "Bloque de imagen con pie de foto"
            };
            el.AddPropertyGroup("content", "Content");
            el.AddPropertyType(
                PropDt(_shortStringHelper, singleImgDt, p =>
                {
                    p.Alias     = "image";
                    p.Name      = "Image";
                    p.Mandatory = true;
                }), "content");
            el.AddPropertyType(
                await PropAsync("Umbraco.TextBox", p =>
                {
                    p.Alias = "caption";
                    p.Name  = "Caption";
                }), "content");
            await _contentTypeService.CreateAsync(el, Constants.Security.SuperUserKey);
        }

        private async Task EnsureQuoteBlockAsync()
        {
            if (_contentTypeService.Get("quoteBlock") != null) return;

            var el = new ContentType(_shortStringHelper, -1)
            {
                Alias       = "quoteBlock",
                Name        = "Quote Block",
                Icon        = "icon-quote",
                IsElement   = true,
                Description = "Bloque de cita con atribución de autor"
            };
            el.AddPropertyGroup("content", "Content");
            el.AddPropertyType(
                await PropAsync("Umbraco.TextArea", p =>
                {
                    p.Alias     = "quote";
                    p.Name      = "Quote";
                    p.Mandatory = true;
                }), "content");
            el.AddPropertyType(
                await PropAsync("Umbraco.TextBox", p =>
                {
                    p.Alias = "autor";
                    p.Name  = "Autor";
                }), "content");
            await _contentTypeService.CreateAsync(el, Constants.Security.SuperUserKey);
        }

        private async Task EnsureFileBlockAsync()
        {
            if (_contentTypeService.Get("fileBlock") != null) return;

            var el = new ContentType(_shortStringHelper, -1)
            {
                Alias       = "fileBlock",
                Name        = "File Block",
                Icon        = "icon-document",
                IsElement   = true,
                Description = "Bloque de archivo descargable"
            };
            el.AddPropertyGroup("content", "Content");
            el.AddPropertyType(
                await PropAsync("Umbraco.MediaPicker3", p =>
                {
                    p.Alias     = "file";
                    p.Name      = "File";
                    p.Mandatory = true;
                }), "content");
            el.AddPropertyType(
                await PropAsync("Umbraco.TextBox", p =>
                {
                    p.Alias = "label";
                    p.Name  = "Label";
                }), "content");
            await _contentTypeService.CreateAsync(el, Constants.Security.SuperUserKey);
        }

        // ── DataType helpers ──────────────────────────────────────────────────

        private async Task<IDataType> GetOrCreateCategoryDropdownDataTypeAsync()
        {
            const string name = "Categoría Editorial";
            var existing = await DtByNameAsync(name);
            if (existing != null) return existing;

            if (!_propertyEditors.TryGet("Umbraco.DropDown.Flexible", out var editor))
                throw new InvalidOperationException("Editor 'Umbraco.DropDown.Flexible' no encontrado.");

            var dt = new DataType(editor, _serializer)
            {
                Name         = name,
                DatabaseType = ValueStorageType.Nvarchar,
                ConfigurationData = new Dictionary<string, object>
                {
                    ["multiple"] = false,
                    ["items"] = new[]
                    {
                        new { id = 1, value = "Artículo" },
                        new { id = 2, value = "Ensayo"   },
                        new { id = 3, value = "Reseña"   },
                        new { id = 4, value = "Tutorial" }
                    }
                }
            };
            await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            return dt;
        }

        private async Task<IDataType> GetOrCreateSingleImagePickerDataTypeAsync()
        {
            const string name = "Single Image Picker";
            var existing = await DtByNameAsync(name);
            if (existing != null) return existing;

            if (!_propertyEditors.TryGet("Umbraco.MediaPicker3", out var editor))
                throw new InvalidOperationException("Editor 'Umbraco.MediaPicker3' no encontrado.");

            var dt = new DataType(editor, _serializer)
            {
                Name         = name,
                DatabaseType = ValueStorageType.Ntext,
                ConfigurationData = new Dictionary<string, object>
                {
                    ["multiple"]        = false,
                    ["validationLimit"] = new { min = 0, max = 1 },
                    ["filter"]          = "Image",
                    ["crops"]           = Array.Empty<object>()
                }
            };
            await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            return dt;
        }

        private async Task<IDataType> GetOrCreateContenidoBlockListDataTypeAsync()
        {
            const string name = "Contenido Editorial - Block List";
            var existing = await DtByNameAsync(name);
            if (existing != null) return existing;

            var textBlock  = _contentTypeService.Get("textBlock")  ?? throw new InvalidOperationException("textBlock element type not found.");
            var imageBlock = _contentTypeService.Get("imageBlock") ?? throw new InvalidOperationException("imageBlock element type not found.");
            var quoteBlock = _contentTypeService.Get("quoteBlock") ?? throw new InvalidOperationException("quoteBlock element type not found.");
            var fileBlock  = _contentTypeService.Get("fileBlock")  ?? throw new InvalidOperationException("fileBlock element type not found.");

            if (!_propertyEditors.TryGet("Umbraco.BlockList", out var editor))
                throw new InvalidOperationException("Editor 'Umbraco.BlockList' no encontrado.");

            var dt = new DataType(editor, _serializer)
            {
                Name         = name,
                DatabaseType = ValueStorageType.Ntext,
                ConfigurationData = new Dictionary<string, object>
                {
                    ["blocks"] = new[]
                    {
                        new { contentElementTypeKey = textBlock.Key,  label = "Text Block"  },
                        new { contentElementTypeKey = imageBlock.Key, label = "Image Block" },
                        new { contentElementTypeKey = quoteBlock.Key, label = "Quote Block" },
                        new { contentElementTypeKey = fileBlock.Key,  label = "File Block"  }
                    },
                    ["useSingleBlockMode"]        = false,
                    ["useLiveEditing"]            = false,
                    ["useInlineEditingAsDefault"] = false
                }
            };
            await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            return dt;
        }

        // ── EditorialContent ──────────────────────────────────────────────────

        private async Task CreateEditorialContentDocumentTypeAsync()
        {
            const string alias = "editorialContent";
            if (_contentTypeService.Get(alias) != null) return;

            var categoryDt  = await GetOrCreateCategoryDropdownDataTypeAsync();
            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();
            var contenidoDt = await GetOrCreateContenidoBlockListDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Editorial Content",
                Description   = "Contenido editorial con soporte EPUB/PDF",
                Icon          = "icon-newspaper",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");

            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p =>
            {
                p.Alias     = "author";
                p.Name      = "Autor";
                p.Mandatory = true;
            }), "content");

            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p =>
            {
                p.Alias     = "title";
                p.Name      = "Título";
                p.Mandatory = true;
            }), "content");

            doc.AddPropertyType(await PropAsync("Umbraco.Tags", p =>
            {
                p.Alias     = "keywords";
                p.Name      = "Palabras clave";
                p.Mandatory = true;
            }), "content");

            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt, p =>
            {
                p.Alias       = "imagenDestacada";
                p.Name        = "Imagen Destacada";
                p.Description = "Imagen principal del artículo (solo imágenes)";
            }), "content");

            doc.AddPropertyType(PropDt(_shortStringHelper, contenidoDt, p =>
            {
                p.Alias       = "content";
                p.Name        = "Contenido";
                p.Description = "Bloques de contenido: texto, imagen, cita o archivo";
                p.Mandatory   = true;
            }), "content");

            doc.AddPropertyType(await PropAsync("Umbraco.MediaPicker3", p =>
            {
                p.Alias       = "pdfEpub";
                p.Name        = "PDF/EPUB";
                p.Description = "Archivo para descarga directa";
            }), "content");

            doc.AddPropertyType(await PropAsync("Umbraco.DatePicker", p =>
            {
                p.Alias     = "date";
                p.Name      = "Fecha";
                p.Mandatory = true;
            }), "content");

            doc.AddPropertyType(PropDt(_shortStringHelper, categoryDt, p =>
            {
                p.Alias       = "category";
                p.Name        = "Categoría";
                p.Description = "Artículo | Ensayo | Reseña | Tutorial";
                p.Mandatory   = true;
            }), "content");

            doc.AddPropertyType(await PropAsync("Umbraco.ContentPicker", p =>
            {
                p.Alias       = "epubRelacionado";
                p.Name        = "EPUB Relacionado";
                p.Description = "Nodo EPUBViewer vinculado a este artículo";
            }), "content");

            doc.AddPropertyGroup("seo", "SEO");

            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p =>
            {
                p.Alias       = "seoTitle";
                p.Name        = "SEO Title";
                p.Description = "Máximo 60 caracteres";
            }), "seo");

            doc.AddPropertyType(await PropAsync("Umbraco.TextArea", p =>
            {
                p.Alias       = "seoDescription";
                p.Name        = "SEO Description";
                p.Description = "Máximo 160 caracteres";
            }), "seo");

            doc.AddPropertyType(await PropAsync("Umbraco.MediaPicker3", p =>
            {
                p.Alias = "socialImage";
                p.Name  = "Social Sharing Image";
            }), "seo");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── EPUBViewer ────────────────────────────────────────────────────────

        private async Task CreateEPUBViewerDocumentTypeAsync()
        {
            const string alias = "epubViewer";
            if (_contentTypeService.Get(alias) != null) return;

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "EPUB Viewer",
                Description   = "Visualización de EPUB con soporte multilenguaje",
                Icon          = "icon-book",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");

            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",      p => { p.Alias = "title";           p.Name = "Título";            p.Mandatory = true; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.MediaPicker3", p => { p.Alias = "epubFile";        p.Name = "EPUB File";         p.Mandatory = true; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",      p => { p.Alias = "language";        p.Name = "Idioma";            p.Mandatory = true; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",      p => { p.Alias = "author";          p.Name = "Autor"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",      p => { p.Alias = "publisher";       p.Name = "Editorial"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.DatePicker",   p => { p.Alias = "publicationDate"; p.Name = "Fecha Publicación"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextArea",     p => { p.Alias = "description";     p.Name = "Descripción"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Tags",         p => { p.Alias = "tags";            p.Name = "Etiquetas"; }), "content");

            doc.AddPropertyGroup("seo", "SEO");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",      p => { p.Alias = "seoTitle";        p.Name = "SEO Title";         p.Description = "Máximo 60 caracteres"; }), "seo");
            doc.AddPropertyType(await PropAsync("Umbraco.TextArea",     p => { p.Alias = "seoDescription";  p.Name = "SEO Description";   p.Description = "Máximo 160 caracteres"; }), "seo");
            doc.AddPropertyType(await PropAsync("Umbraco.MediaPicker3", p => { p.Alias = "socialImage";     p.Name = "Social Sharing Image"; }), "seo");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── Azure AI Search ───────────────────────────────────────────────────

        private async Task CreateAzureAISearchDataTypesAsync()
        {
            if (!_propertyEditors.TryGet("Umbraco.TextBox", out var textEditor)) return;

            const string configName = "Azure AI Search Config";
            if (await DtByNameAsync(configName) == null)
            {
                var dt = new DataType(textEditor, _serializer) { Name = configName, DatabaseType = ValueStorageType.Ntext };
                await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            }

            const string searchName = "Azure AI Search";
            if (await DtByNameAsync(searchName) == null)
            {
                var dt = new DataType(textEditor, _serializer) { Name = searchName, DatabaseType = ValueStorageType.Ntext };
                await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            }
        }

        // ── EcosystemProject ──────────────────────────────────────────────────

        private async Task CreateEcosystemProjectDocumentTypeAsync()
        {
            const string alias = "ecosystemProject";
            if (_contentTypeService.Get(alias) != null) return;

            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Ecosystem Project",
                Description   = "Used by Homepage + Ecosistema page",
                Icon          = "icon-science",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "title";       p.Name = "Title"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "slug";        p.Name = "Slug"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextArea", p => { p.Alias = "description"; p.Name = "Description"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "category";    p.Name = "Category"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "color";       p.Name = "Color"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TrueFalse",p => { p.Alias = "active";      p.Name = "Active"; }),      "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt, p => { p.Alias = "image";       p.Name = "Image"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Tags",     p => { p.Alias = "details";     p.Name = "Details"; }),     "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "status";      p.Name = "Status"; }),      "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "launched";    p.Name = "Launched"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "href";        p.Name = "Href"; }),        "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── Article ───────────────────────────────────────────────────────────

        private async Task CreateArticleDocumentTypeAsync()
        {
            const string alias = "article";
            if (_contentTypeService.Get(alias) != null) return;

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Article",
                Description   = "Used by Editorial page",
                Icon          = "icon-newspaper",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",   p => { p.Alias = "title";       p.Name = "Title"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",   p => { p.Alias = "slug";        p.Name = "Slug"; }),         "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextArea",  p => { p.Alias = "excerpt";     p.Name = "Excerpt"; }),      "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TinyMCE",   p => { p.Alias = "content";     p.Name = "Content"; }),      "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",   p => { p.Alias = "category";    p.Name = "Category"; }),     "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Number",    p => { p.Alias = "readTime";    p.Name = "Read Time"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.DatePicker",p => { p.Alias = "publishDate"; p.Name = "Publish Date"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TrueFalse", p => { p.Alias = "featured";    p.Name = "Featured"; }),     "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Tags",      p => { p.Alias = "tags";        p.Name = "Tags"; }),         "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",   p => { p.Alias = "author";      p.Name = "Author"; }),       "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── Book ──────────────────────────────────────────────────────────────

        private async Task CreateBookDocumentTypeAsync()
        {
            const string alias = "book";
            if (_contentTypeService.Get(alias) != null) return;

            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();
            var epubFileDt  = await GetOrCreateEpubMediaPickerDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Book",
                Description   = "Used by Editorial + CiriWhispers",
                Icon          = "icon-book",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "title";       p.Name = "Title"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "slug";        p.Name = "Slug"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TinyMCE", p => { p.Alias = "description"; p.Name = "Description"; }), "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt,               p => { p.Alias = "cover";       p.Name = "Cover"; }),       "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, epubFileDt,                 p => { p.Alias = "epubFile";    p.Name = "EPUB File"; }),   "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "author";      p.Name = "Author"; }),      "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "genre";       p.Name = "Genre"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "status";      p.Name = "Status"; }),      "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── Property ──────────────────────────────────────────────────────────

        private async Task CreatePropertyDocumentTypeAsync()
        {
            const string alias = "property";
            if (_contentTypeService.Get(alias) != null) return;

            var multiImgDt = await GetOrCreateMultipleImagePickerDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Property",
                Description   = "Used by MaalCa Properties page",
                Icon          = "icon-home",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "title";       p.Name = "Title"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Decimal",  p => { p.Alias = "price";       p.Name = "Price"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "location";    p.Name = "Location"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Number",   p => { p.Alias = "beds";        p.Name = "Beds"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Number",   p => { p.Alias = "baths";       p.Name = "Baths"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Number",   p => { p.Alias = "sqft";        p.Name = "Sqft"; }),        "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, multiImgDt,                  p => { p.Alias = "images";      p.Name = "Images"; }),      "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "type";        p.Name = "Type"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Tags",     p => { p.Alias = "features";    p.Name = "Features"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TinyMCE",  p => { p.Alias = "description"; p.Name = "Description"; }), "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── DoctorProfile ─────────────────────────────────────────────────────

        private async Task CreateDoctorProfileDocumentTypeAsync()
        {
            const string alias = "doctorProfile";
            if (_contentTypeService.Get(alias) != null) return;

            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Doctor Profile",
                Description   = "Used by Dr. Pichardo page",
                Icon          = "icon-doctor",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "name";         p.Name = "Name"; }),         "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TinyMCE",  p => { p.Alias = "bio";          p.Name = "Bio"; }),          "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "license";      p.Name = "License"; }),      "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt,                 p => { p.Alias = "photo";        p.Name = "Photo"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Tags",     p => { p.Alias = "specialties";  p.Name = "Specialties"; }),  "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "contactPhone"; p.Name = "Contact Phone"; }),"content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "address";      p.Name = "Address"; }),      "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── CiriSonicService ──────────────────────────────────────────────────

        private async Task CreateCiriSonicServiceDocumentTypeAsync()
        {
            const string alias = "ciriSonicService";
            if (_contentTypeService.Get(alias) != null) return;

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Ciri Sonic Service",
                Description   = "Used by CiriSonic page",
                Icon          = "icon-wand",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "title";       p.Name = "Title"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TinyMCE",  p => { p.Alias = "description"; p.Name = "Description"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "icon";        p.Name = "Icon"; }),        "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "price";       p.Name = "Price"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox",  p => { p.Alias = "category";    p.Name = "Category"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TrueFalse",p => { p.Alias = "featured";    p.Name = "Featured"; }),    "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── AffiliateLanding ──────────────────────────────────────────────────

        private async Task CreateAffiliateLandingDocumentTypeAsync()
        {
            const string alias = "affiliateLanding";
            if (_contentTypeService.Get(alias) != null) return;

            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Affiliate Landing",
                Description   = "Used by Dashboard public landing pages",
                Icon          = "icon-link",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "affiliateId";    p.Name = "Affiliate ID"; }),     "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt,                p => { p.Alias = "logo";           p.Name = "Logo"; }),             "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt,                p => { p.Alias = "heroImage";      p.Name = "Hero Image"; }),       "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextArea",p => { p.Alias = "description";    p.Name = "Description"; }),      "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "primaryColor";   p.Name = "Primary Color"; }),    "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "secondaryColor"; p.Name = "Secondary Color"; }), "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── Testimonial ───────────────────────────────────────────────────────

        private async Task CreateTestimonialDocumentTypeAsync()
        {
            const string alias = "testimonial";
            if (_contentTypeService.Get(alias) != null) return;

            var singleImgDt = await GetOrCreateSingleImagePickerDataTypeAsync();

            var doc = new ContentType(_shortStringHelper, -1)
            {
                Alias         = alias,
                Name          = "Testimonial",
                Description   = "Used by Affiliate landing pages",
                Icon          = "icon-chat",
                AllowedAsRoot = true,
                IsElement     = false
            };

            doc.AddPropertyGroup("content", "Content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "customerName"; p.Name = "Customer Name"; }), "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TinyMCE", p => { p.Alias = "quote";        p.Name = "Quote"; }),         "content");
            doc.AddPropertyType(await PropAsync("Umbraco.Number",  p => { p.Alias = "rating";       p.Name = "Rating"; }),        "content");
            doc.AddPropertyType(PropDt(_shortStringHelper, singleImgDt,                p => { p.Alias = "photo";        p.Name = "Photo"; }),         "content");
            doc.AddPropertyType(await PropAsync("Umbraco.TextBox", p => { p.Alias = "affiliateId";  p.Name = "Affiliate ID"; }),  "content");

            await _contentTypeService.CreateAsync(doc, Constants.Security.SuperUserKey);
        }

        // ── Content Folders ───────────────────────────────────────────────────

        private async Task CreateContentFoldersAsync()
        {
            var folderContentType = _contentTypeService.Get("Folder");
            if (folderContentType == null) return;

            var folders = new Dictionary<string, string>
            {
                { "ecosystem-projects", "Ecosystem Projects" },
                { "articles",           "Articles"           },
                { "books",              "Books"              },
                { "properties",         "Properties"         },
                { "affiliate-landings", "Affiliate Landings" },
                { "testimonials",       "Testimonials"       }
            };

            foreach (var (key, name) in folders)
            {
                var existing = _contentService.GetRootContent()
                    .FirstOrDefault(c => c.Name == name || c.Key.ToString() == key);

                if (existing != null) continue;

                var folder = _contentService.Create(name, -1, "Folder");
                folder.Key = Guid.TryParse(key, out var g) ? g : Guid.NewGuid();
                _contentService.Save(folder);
            }

            await Task.CompletedTask;
        }

        // ── DataType Helpers ──────────────────────────────────────────────────

        private async Task<IDataType> GetOrCreateEpubMediaPickerDataTypeAsync()
        {
            const string name = "EPUB Media Picker";
            var existing = await DtByNameAsync(name);
            if (existing != null) return existing;

            if (!_propertyEditors.TryGet("Umbraco.MediaPicker3", out var editor))
                throw new InvalidOperationException("Editor 'Umbraco.MediaPicker3' not found.");

            var dt = new DataType(editor, _serializer)
            {
                Name         = name,
                DatabaseType = ValueStorageType.Ntext,
                ConfigurationData = new Dictionary<string, object>
                {
                    ["multiple"]        = false,
                    ["validationLimit"] = new { min = 0, max = 1 },
                    ["filter"]          = "File",
                    ["crops"]           = Array.Empty<object>()
                }
            };
            await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            return dt;
        }

        private async Task<IDataType> GetOrCreateMultipleImagePickerDataTypeAsync()
        {
            const string name = "Multiple Image Picker";
            var existing = await DtByNameAsync(name);
            if (existing != null) return existing;

            if (!_propertyEditors.TryGet("Umbraco.MediaPicker3", out var editor))
                throw new InvalidOperationException("Editor 'Umbraco.MediaPicker3' not found.");

            var dt = new DataType(editor, _serializer)
            {
                Name         = name,
                DatabaseType = ValueStorageType.Ntext,
                ConfigurationData = new Dictionary<string, object>
                {
                    ["multiple"]        = true,
                    ["validationLimit"] = new { min = 0, max = 10 },
                    ["filter"]          = "Image",
                    ["crops"]           = Array.Empty<object>()
                }
            };
            await _dataTypeService.CreateAsync(dt, Constants.Security.SuperUserKey);
            return dt;
        }
    }
}
