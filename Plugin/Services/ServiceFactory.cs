namespace NavisBOQ.Plugin.Services
{
    public static class ServiceFactory
    {
        private static readonly IModelStructurePolicyService _modelStructurePolicyService = new ModelStructurePolicyService();
        private static readonly IModelCategoryAliasService _modelCategoryAliasService = new ModelCategoryAliasService();
        private static readonly IPropertyReaderService _propertyReaderService = new PropertyReaderService(_modelStructurePolicyService);

        private static readonly INodeResolutionPolicyService _nodeResolutionPolicyService =
            new NodeResolutionPolicyService(_propertyReaderService, _modelCategoryAliasService, _modelStructurePolicyService);

        private static readonly ISnapshotService _snapshotService =
            new SnapshotService(_propertyReaderService, _nodeResolutionPolicyService, _modelCategoryAliasService);

        private static readonly ISelectionScopeService _selectionScopeService =
            new SelectionScopeService(_propertyReaderService, _snapshotService);

        private static readonly IQuantityExtractionService _quantityExtractionService =
            new QuantityExtractionService(_selectionScopeService, _snapshotService);

        private static readonly IQuantityMapperService _quantityMapperService = new QuantityMapperService();
        private static readonly ISteelWeightService _steelWeightService = new SteelWeightService(_modelCategoryAliasService);
        private static readonly IBoqAggregationService _boqAggregationService = new BoqAggregationService();
        private static readonly IExecutionModePolicyService _executionModePolicyService = new ExecutionModePolicyService();

        private static readonly ISelectionSetValidationService _selectionSetValidationService =
            new SelectionSetValidationService(_selectionScopeService, _propertyReaderService, _modelCategoryAliasService);

        private static readonly IPreconstruccion1Service _preconstruccion1Service =
            new Preconstruccion1Service(
                _selectionScopeService,
                _quantityExtractionService,
                _quantityMapperService,
                _boqAggregationService,
                _executionModePolicyService);

        private static readonly IPreconstruccion2Service _preconstruccion2Service =
            new Preconstruccion2Service(
                _selectionScopeService,
                _quantityExtractionService,
                _quantityMapperService,
                _boqAggregationService,
                _executionModePolicyService);

        private static readonly IPreconstruccion3Service _preconstruccion3Service =
            new Preconstruccion3Service(
                _selectionScopeService,
                _quantityExtractionService,
                _steelWeightService,
                _boqAggregationService,
                _executionModePolicyService,
                _selectionSetValidationService);

        // ===== NUEVO: Corrida 4 =====
        private static readonly IElectricalCategoryClassifierService _electricalCategoryClassifierService =
            new ElectricalCategoryClassifierService();

        private static readonly IElectricalQuantityMapperService _electricalQuantityMapperService =
            new ElectricalQuantityMapperService();

        private static readonly IElectricalAggregationService _electricalAggregationService =
            new ElectricalAggregationService();

        private static readonly IPreconstruccion4Service _preconstruccion4Service =
            new Preconstruccion4Service(
                _selectionScopeService,
                _quantityExtractionService,
                _electricalCategoryClassifierService,
                _electricalQuantityMapperService,
                _electricalAggregationService,
                _executionModePolicyService,
                _selectionSetValidationService);

        private static readonly IDetailExpansionPolicyService _detailExpansionPolicyService =
            new DetailExpansionPolicyService();

        private static readonly IDetailFieldProfileService _electricalDetailFieldProfileService =
            new ElectricalDetailFieldProfileService();

        private static readonly IElectricalDetailExtractionService _electricalDetailExtractionService =
            new ElectricalDetailExtractionService(
                _selectionScopeService,
                _snapshotService,
                _propertyReaderService,
                _electricalCategoryClassifierService,
                _electricalDetailFieldProfileService,
                _detailExpansionPolicyService);

        public static IModelStructurePolicyService CreateModelStructurePolicyService() => _modelStructurePolicyService;
        public static IPropertyReaderService CreatePropertyReaderService() => _propertyReaderService;
        public static IModelCategoryAliasService CreateModelCategoryAliasService() => _modelCategoryAliasService;
        public static INodeResolutionPolicyService CreateNodeResolutionPolicyService() => _nodeResolutionPolicyService;
        public static ISnapshotService CreateSnapshotService() => _snapshotService;
        public static ISelectionScopeService CreateSelectionScopeService() => _selectionScopeService;
        public static IQuantityExtractionService CreateQuantityExtractionService() => _quantityExtractionService;
        public static IQuantityMapperService CreateQuantityMapperService() => _quantityMapperService;
        public static ISteelWeightService CreateSteelWeightService() => _steelWeightService;
        public static IBoqAggregationService CreateBoqAggregationService() => _boqAggregationService;
        public static IExecutionModePolicyService CreateExecutionModePolicyService() => _executionModePolicyService;
        public static ISelectionSetValidationService CreateSelectionSetValidationService() => _selectionSetValidationService;
        public static IPreconstruccion1Service CreatePreconstruccion1Service() => _preconstruccion1Service;
        public static IPreconstruccion2Service CreatePreconstruccion2Service() => _preconstruccion2Service;
        public static IPreconstruccion3Service CreatePreconstruccion3Service() => _preconstruccion3Service;

        // ===== NUEVO =====
        public static IPreconstruccion4Service CreatePreconstruccion4Service() => _preconstruccion4Service;

        public static IElectricalDetailExtractionService CreateElectricalDetailExtractionService() => _electricalDetailExtractionService;
    }
}
