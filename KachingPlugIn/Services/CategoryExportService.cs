using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using KachingPlugIn.Factories;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using Mediachase.Commerce.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KachingPlugIn.Services
{
    class FoldersAndTags
    {
        internal List<Folder> Folders { get; private set; }
        internal IDictionary<string, Tag> Tags { get; private set; }

        internal FoldersAndTags()
        {
            Folders = new List<Folder>();
            Tags = new Dictionary<string, Tag>();
        }
    }

    public class CategoryExportService
    {
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly L10nStringFactory _l10nStringFactory;
        private readonly ILogger _log = LogManager.GetLogger(typeof(CategoryExportService));

        public IExportState ExportState { get; set; }
        public CategoryExportService(
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            IContentVersionRepository contentVersionRepository,
            L10nStringFactory l10nStringFactory)
        {
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
            _contentVersionRepository = contentVersionRepository;
            _l10nStringFactory = l10nStringFactory;
        }

        public void StartFullCategoryExport(string tagsUrl, string foldersUrl)
        {
            if (ExportState != null)
            {
                if (ExportState.Busy)
                {
                    return;
                }
                ExportState.Busy = true;
            }

            Task.Run(() => {
                try
                {
                    ExportCategoryStructure(tagsUrl, foldersUrl);
                }
                catch (WebException e)
                {
                    ResetState(true);
                    _log.Error("Response status code: " + e.Status);
                    _log.Error("Export aborted with error: " + e.Message);
                }
                catch (Exception e)
                {
                    ResetState(true);
                    _log.Error("Export aborted with error: " + e.Message);
                }
            });
        }

        private void ExportCategoryStructure(string tagsUrl, string foldersUrl)
        {
            var root = _contentLoader.GetChildren<CatalogContent>(_referenceConverter.GetRootLink());
            var first = root.FirstOrDefault();

            var children = _contentLoader.GetChildren<NodeContent>(first.ContentLink);

            var result = BuildTagsAndFolders(children);

            if (ExportState != null)
            {
                ExportState.Total = result.Folders.Aggregate(0, (a, f) => a + f.CountNodesInTree()) + result.Tags.Count;
            }

            Post(result.Tags.Values.ToList(), tagsUrl);

            if (ExportState != null)
            {
                ExportState.Uploaded += result.Tags.Count;
            }

            Post(result.Folders, foldersUrl);

            ResetState(false);
        }

        private FoldersAndTags BuildTagsAndFolders(IEnumerable<NodeContent> nodes)
        {
            var result = new FoldersAndTags();

            foreach (var node in nodes)
            {
                // continue if not published
                var isPublished = _contentVersionRepository.ListPublished(node.ContentLink).Count() > 0;
                if (!isPublished)
                {
                    _log.Information("Skipped product because it's not yet published");
                    continue;
                }

                var tag = new Tag();

                tag.TagValue = node.Code.SanitizeKey();

                tag.Name = _l10nStringFactory.GetLocalizedString(node, nameof(node.DisplayName));

                _log.Information("Category code: " + node.Code);

                result.Tags[tag.TagValue] = tag;

                var folder = new Folder(tag.TagValue);
                result.Folders.Add(folder);

                var childrenNodes = _contentLoader.GetChildren<NodeContent>(node.ContentLink);
                if (childrenNodes.Count() > 0)
                {
                    var childrenFoldersAndTags = BuildTagsAndFolders(childrenNodes);
                    folder.Children = childrenFoldersAndTags.Folders;
                    foreach (var pair in childrenFoldersAndTags.Tags)
                    {
                        result.Tags[pair.Key] = pair.Value;
                    }
                }
            }

            return result;
        }

        private void Post<T>(List<T> entitiesList, string url)
        {
            var statusCode = APIFacade.Post(entitiesList, url);
            _log.Information("Status code: " + statusCode.ToString());
        }

        private void ResetState(bool error)
        {
            if (ExportState == null)
            {
                return;
            }

            ExportState.Busy = false;
            ExportState.Total = 0;
            ExportState.Uploaded = 0;
            ExportState.Polls = 0;
            ExportState.Error = error;
        }
    }
}