using System.Collections.Generic;
using System.Linq;
using CitizenMatt.ReSharper.Plugins.Clippy.AgentApi;
using JetBrains.Application.DataContext;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Navigation.Core.RecentFiles;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace CitizenMatt.ReSharper.Plugins.Clippy.OverriddenActions
{
    public class GotoRecentEditsAction(Lifetime lifetime,
                                       Agent agent,
                                       ISolution solution,
                                       IShellLocks shellLocks,
                                       IPsiFiles psiFiles,
                                       RecentFilesTracker tracker,
                                       OccurrencePresentationManager presentationManager,
                                       ProjectModelElementPointerManager projectModelElementPointerManager,
                                       IMainWindowPopupWindowContext popupWindowContext)
        : GotoRecentActionBase(lifetime, agent, solution, shellLocks, psiFiles, tracker, presentationManager,
            projectModelElementPointerManager, popupWindowContext), IExecutableAction
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            return context.CheckAllNotNull(ProjectModelDataConstants.SOLUTION);
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            using(ReadLockCookie.Create())
            {
                PsiFiles.CommitAllDocuments();
                ShowLocations(Tracker.GetEditLocations(), Tracker.CurrentEdit, "Recent Edits", true);
            }
        }
    }

    public class GotoRecentFilesAction(Lifetime lifetime,
                                       Agent agent,
                                       ISolution solution,
                                       IShellLocks shellLocks,
                                       IPsiFiles psiFiles,
                                       RecentFilesTracker tracker,
                                       OccurrencePresentationManager presentationManager,
                                       ProjectModelElementPointerManager projectModelElementPointerManager,
                                       IMainWindowPopupWindowContext popupWindowContext)
        : GotoRecentActionBase(lifetime, agent, solution, shellLocks, psiFiles, tracker, presentationManager,
            projectModelElementPointerManager, popupWindowContext), IExecutableAction
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            // We can't call nextUpdate, as that means Execute never gets called
            return context.CheckAllNotNull(ProjectModelDataConstants.SOLUTION);
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            ShowLocations(Tracker.GetFileLocations(), Tracker.CurrentFile, "Recent Files", false);
        }
    }

    public class GotoRecentActionBase
    {
        private readonly Lifetime lifetime;
        private readonly Agent agent;
        private readonly ISolution solution;
        private readonly IShellLocks shellLocks;
        private readonly OccurrencePresentationManager presentationManager;
        private readonly ProjectModelElementPointerManager projectModelElementPointerManager;
        private readonly IMainWindowPopupWindowContext popupWindowContext;

        protected GotoRecentActionBase(Lifetime lifetime, Agent agent, ISolution solution,
            IShellLocks shellLocks, IPsiFiles psiFiles, RecentFilesTracker tracker,
            OccurrencePresentationManager presentationManager,
            ProjectModelElementPointerManager projectModelElementPointerManager,
            IMainWindowPopupWindowContext popupWindowContext)
        {
            this.lifetime = lifetime;
            this.agent = agent;
            this.solution = solution;
            this.shellLocks = shellLocks;
            PsiFiles = psiFiles;
            Tracker = tracker;
            this.presentationManager = presentationManager;
            this.projectModelElementPointerManager = projectModelElementPointerManager;
            this.popupWindowContext = popupWindowContext;
        }

        protected RecentFilesTracker Tracker { get; }

        protected IPsiFiles PsiFiles { get; }


        protected void ShowLocations(IReadOnlyList<FileLocationInfo> locations, FileLocationInfo currentLocation, string caption, bool bindToPsi)
        {
            var lifetimeDefinition = lifetime.CreateNested();

            if (!locations.Any())
            {
                agent.ShowBalloon(lifetimeDefinition.Lifetime, caption,
                    $"There are no {caption.ToLowerInvariant()}.", null,
                    ["OK"], false, balloonLifetime =>
                    {
                        agent.ButtonClicked.Advise(balloonLifetime, _ => lifetimeDefinition.Terminate());
                    });
                return;
            }

            var options = new List<BalloonOption>();
            foreach (var locationInfo in locations.Distinct().Where(l => l.GetProjectFile(projectModelElementPointerManager)?.IsValid() == true || l.FileSystemPath != null))
            {
                var descriptor = new SimpleMenuItem();

                var occurence = GetOccurence(locationInfo, bindToPsi);
                if (occurence != null)
                {
                    presentationManager.DescribeOccurrence(descriptor, occurence);
                    var enabled = locationInfo != currentLocation;
                    options.Add(new BalloonOption(
                        (descriptor.Text + $" ({descriptor.ShortcutText})").Text, false, enabled,
                        locationInfo));
                }
            }

            agent.ShowBalloon(lifetimeDefinition.Lifetime, caption, string.Empty, options, new[] { "Done" }, true,
                balloonLifetime =>
                {
                    agent.BalloonOptionClicked.Advise(balloonLifetime, o =>
                    {
                        lifetimeDefinition.Terminate();

                        var locationInfo = o as FileLocationInfo;
                        if (locationInfo == null)
                            return;

                        shellLocks.ExecuteOrQueueReadLock("GotoRecentFiles", () =>
                        {
                            using (ReadLockCookie.Create())
                            {
                                PsiFiles.CommitAllDocuments();
                                var occurence = GetOccurence(locationInfo, bindToPsi);
                                occurence?.Navigate(solution, popupWindowContext.Source, true);
                            }
                        });
                    });

                    agent.ButtonClicked.Advise(balloonLifetime, _ => lifetimeDefinition.Terminate());
                });
        }

        private IOccurrence GetOccurence(FileLocationInfo location, bool bindToPsi)
        {
            var projectFile = location.GetProjectFile(projectModelElementPointerManager);
            if (projectFile == null || projectFile.GetProject() == null)
            {
                return new ExternalSourceOccurrence(location.FileSystemPath, new TextRange(location.CaretOffset), location.CachedPresentation, solution);
            }

            if (bindToPsi)
            {
                var sourceFile = projectFile.ToSourceFile();
                if (sourceFile != null)
                {
                    var document = sourceFile.Document;
                    var documentRange = document.DocumentRange;
                    int offset;
                    if (documentRange.Contains(location.CaretOffset))
                    {
                        offset = location.CaretOffset;
                    }
                    else
                    {
                        offset = documentRange.EndOffset;
                    }

                    ContainingMemberManager.GetInstance(sourceFile).BindToPsi(sourceFile, KnownLanguage.ANY,
                        new TextRange(offset), out var boundTypeMember, out var boundTypeElement, out var _,
                        PsiLanguageCategories.Dominant);
                    var options = new OccurrencePresentationOptions { ContainerStyle = ContainerDisplayStyle.File };
                    if (boundTypeMember != null || boundTypeElement != null)
                    {
                        var declaredElement = (boundTypeMember ?? boundTypeElement).GetValidDeclaredElement();
                        if (declaredElement != null)
                        {
                            return new CustomRangeOccurrence(sourceFile, new DocumentRange(sourceFile.Document, new TextRange(location.CaretOffset)), options);
                        }
                    }
                    else
                    {
                        return new CustomRangeOccurrence(sourceFile, new DocumentRange(document, offset), options);
                    }
                }
            }

            //project file can loose its owner project (i.e. micsFilesProject) during provision tab navigation
            if (projectFile.IsValid())
                return new ProjectItemOccurrence(projectFile, new OccurrencePresentationOptions { ContainerStyle = ContainerDisplayStyle.NoContainer });

            return null;
        }
    }
}