﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using PoESkillTree.Computation.Common.Builders;
using PoESkillTree.Computation.Common.Builders.Stats;
using PoESkillTree.GameModel;
using PoESkillTree.GameModel.Skills;
using PoESkillTree.Utils.Extensions;
using POESKillTree.Utils;

namespace POESKillTree.Computation.ViewModels
{
    public class MainSkillSelectionViewModel : Notifier
    {
        private readonly SkillDefinitions _skillDefinitions;
        private readonly ConfigurationNodeViewModel _selectedSkillItemSlot;
        private readonly ConfigurationNodeViewModel _selectedSkillSocketIndex;
        private readonly ConfigurationNodeViewModel _selectedSkillPart;

        private MainSkillViewModel _selectedSkill;

        public static MainSkillSelectionViewModel Create(
            SkillDefinitions skillDefinitions, IBuilderFactories builderFactories,
            CalculationNodeViewModelFactory nodeFactory,
            ObservableCollection<IReadOnlyList<Skill>> skills)
        {
            var vm = new MainSkillSelectionViewModel(skillDefinitions, builderFactories, nodeFactory);
            vm.Initialize(skills);
            return vm;
        }

        private MainSkillSelectionViewModel(
            SkillDefinitions skillDefinitions, IBuilderFactories builderFactories,
            CalculationNodeViewModelFactory nodeFactory)
        {
            _skillDefinitions = skillDefinitions;
            var selectedSkillItemSlotStat = builderFactories.MetaStatBuilders.MainSkillItemSlot
                .BuildToStats(Entity.Character).Single();
            _selectedSkillItemSlot = nodeFactory.CreateConfiguration(selectedSkillItemSlotStat);
            var selectedSkillSocketIndexStat = builderFactories.MetaStatBuilders.MainSkillSocketIndex
                .BuildToStats(Entity.Character).Single();
            _selectedSkillSocketIndex = nodeFactory.CreateConfiguration(selectedSkillSocketIndexStat);
            var selectedSkillPartStat = builderFactories.StatBuilders.MainSkillPart
                .BuildToStats(Entity.Character).Single();
            _selectedSkillPart = nodeFactory.CreateConfiguration(selectedSkillPartStat);
        }

        private void Initialize(ObservableCollection<IReadOnlyList<Skill>> skills)
        {
            _selectedSkillPart.NumericValue = 0;
            ResetSkills(skills);
            skills.CollectionChanged += OnSkillsChanged;
        }

        public ObservableCollection<MainSkillViewModel> AvailableSkills { get; } =
            new ObservableCollection<MainSkillViewModel>();

        public MainSkillViewModel SelectedSkill
        {
            get => _selectedSkill;
            set => SetProperty(ref _selectedSkill, value, onChanging: OnSelectedSkillChanging);
        }

        private void OnSelectedSkillChanging(MainSkillViewModel newValue)
        {
            _selectedSkillItemSlot.NumericValue = (double?) newValue?.Skill.ItemSlot;
            _selectedSkillSocketIndex.NumericValue = newValue?.Skill.SocketIndex;
        }

        private void OnSkillsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetSkills((IEnumerable<IEnumerable<Skill>>) sender);
                return;
            }

            if (args.NewItems != null)
            {
                AddSkills(args.NewItems.Cast<IEnumerable<Skill>>());
            }
            if (args.OldItems != null)
            {
                RemoveSkills(args.OldItems.Cast<IEnumerable<Skill>>());
            }
        }

        private void ResetSkills(IEnumerable<IEnumerable<Skill>> skills)
        {
            AvailableSkills.Clear();
            AddSkills(skills);
        }

        private void AddSkills(IEnumerable<IEnumerable<Skill>> skills)
        {
            foreach (var skill in skills.Flatten().Where(IsActiveSkill))
            {
                AddSkill(CreateSkillViewModel(skill));
            }
        }

        private void RemoveSkills(IEnumerable<IEnumerable<Skill>> skills)
        {
            foreach (var skill in skills.Flatten().Where(IsActiveSkill))
            {
                RemoveSkill(skill);
            }
        }

        private bool IsActiveSkill(Skill skill)
            => skill.IsEnabled && !_skillDefinitions.GetSkillById(skill.Id).IsSupport;

        private void AddSkill(MainSkillViewModel skill)
        {
            if (skill.Skill == Skill.Default && AvailableSkills.Any())
                return;

            var defaultVm = AvailableSkills.FirstOrDefault(x => x.Skill == Skill.Default);
            if (defaultVm != null)
            {
                AvailableSkills.Remove(defaultVm);
            }

            AvailableSkills.Add(skill);
            if (AvailableSkills.Count == 1)
            {
                SelectedSkill = AvailableSkills[0];
            }
        }

        private void RemoveSkill(Skill skill)
        {
            var vm = AvailableSkills.FirstOrDefault(x => x.Skill == skill);
            if (vm is null)
                return;

            var wasSelected = SelectedSkill == vm;
            AvailableSkills.Remove(vm);

            if (AvailableSkills.IsEmpty())
            {
                AddSkill(CreateSkillViewModel(Skill.Default));
            }
            else if (wasSelected)
            {
                SelectedSkill = AvailableSkills[0];
            }
        }

        private MainSkillViewModel CreateSkillViewModel(Skill skill)
        {
            var definition = _skillDefinitions.GetSkillById(skill.Id);
            return new MainSkillViewModel(definition, skill, _selectedSkillPart);
        }
    }
}