{
    "version": "1.0.0",
    "metadata": {
        "model": "portal.observability.dashboard",
        "title": "Portal Observability Dashboard",
        "subtitle": "Observability overview"
    },
    "lenses": {
        "0": {
            "order": 0,
            "parts": {
                "0": {
                    "position": {
                        "x": 0,
                        "y": 0,
                        "colSpan": 2,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [],
                        "type": "Extension/HubsExtension/PartType/MarkdownPart",
                        "settings": {
                            "content": {
                                "settings": {
                                    "title": "Application Insights",
                                    "subtitle": "",
                                    "content": "Applications availabillity and performance",
                                    "markdownSource": 1,
                                    "markdownUri": null
                                }
                            }
                        }
                    }
                },
                "1": {
                    "position": {
                        "x": 2,
                        "y": 0,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "value": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}"
                                                },
                                                "name": "availabilityResults/availabilityPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "resourceDisplayName": "${audit_resource_name_app_insights}",
                                                    "color": "#54A300"
                                                }
                                            }
                                        ],
                                        "title": "Availability",
                                        "titleKind": 2,
                                        "visualization": {
                                            "chartType": 2,
                                            "axisVisualization": {
                                                "y": {
                                                    "isVisible": true,
                                                    "min": 0,
                                                    "max": 100
                                                },
                                                "x": {
                                                    "isVisible": true
                                                }
                                            }
                                        },
                                        "openBladeOnClick": {
                                            "openBlade": true,
                                            "destinationBlade": {
                                                "bladeName": "ResourceMenuBlade",
                                                "parameters": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                    "menuid": "availability"
                                                },
                                                "extensionName": "HubsExtension",
                                                "options": {
                                                    "parameters": {
                                                        "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                        "menuid": "availability"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}"
                                                },
                                                "name": "availabilityResults/availabilityPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "resourceDisplayName": "${audit_resource_name_app_insights}",
                                                    "color": "#54A300"
                                                }
                                            }
                                        ],
                                        "title": "Availability",
                                        "titleKind": 2,
                                        "visualization": {
                                            "chartType": 2,
                                            "axisVisualization": {
                                                "y": {
                                                    "isVisible": true,
                                                    "min": 0,
                                                    "max": 100
                                                },
                                                "x": {
                                                    "isVisible": true
                                                }
                                            },
                                            "disablePinning": true
                                        },
                                        "openBladeOnClick": {
                                            "openBlade": true,
                                            "destinationBlade": {
                                                "bladeName": "ResourceMenuBlade",
                                                "parameters": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                    "menuid": "availability"
                                                },
                                                "extensionName": "HubsExtension",
                                                "options": {
                                                    "parameters": {
                                                        "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                        "menuid": "availability"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "2": {
                    "position": {
                        "x": 5,
                        "y": 0,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "value": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}"
                                                },
                                                "name": "performanceCounters/requestExecutionTime",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "displayName": "HTTP request execution time"
                                                }
                                            }
                                        ],
                                        "title": "Avg HTTP request execution time for ${audit_resource_name_app_insights}",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideSubtitle": false
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            }
                                        },
                                        "timespan": {
                                            "relative": {
                                                "duration": 86400000
                                            },
                                            "showUTCTime": false,
                                            "grain": 1
                                        }
                                    }
                                },
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}"
                                                },
                                                "name": "performanceCounters/requestExecutionTime",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "displayName": "HTTP request execution time"
                                                }
                                            }
                                        ],
                                        "title": "Avg HTTP request execution time for ${audit_resource_name_app_insights}",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideSubtitle": false
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "3": {
                    "position": {
                        "x": 8,
                        "y": 0,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "value": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}"
                                                },
                                                "name": "requests/failed",
                                                "aggregationType": 7,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "resourceDisplayName": "${audit_resource_name_app_insights}",
                                                    "color": "#EC008C"
                                                }
                                            }
                                        ],
                                        "title": "Failed requests",
                                        "titleKind": 2,
                                        "visualization": {
                                            "chartType": 3
                                        },
                                        "openBladeOnClick": {
                                            "openBlade": true,
                                            "destinationBlade": {
                                                "bladeName": "ResourceMenuBlade",
                                                "parameters": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                    "menuid": "failures"
                                                },
                                                "extensionName": "HubsExtension",
                                                "options": {
                                                    "parameters": {
                                                        "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                        "menuid": "failures"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}"
                                                },
                                                "name": "requests/failed",
                                                "aggregationType": 7,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "resourceDisplayName": "${audit_resource_name_app_insights}",
                                                    "color": "#EC008C"
                                                }
                                            }
                                        ],
                                        "title": "Failed requests",
                                        "titleKind": 2,
                                        "visualization": {
                                            "chartType": 3,
                                            "disablePinning": true
                                        },
                                        "openBladeOnClick": {
                                            "openBlade": true,
                                            "destinationBlade": {
                                                "bladeName": "ResourceMenuBlade",
                                                "parameters": {
                                                    "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                    "menuid": "failures"
                                                },
                                                "extensionName": "HubsExtension",
                                                "options": {
                                                    "parameters": {
                                                        "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                        "menuid": "failures"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "4": {
                    "position": {
                        "x": 0,
                        "y": 2,
                        "colSpan": 2,
                        "rowSpan": 4
                    },
                    "metadata": {
                        "inputs": [],
                        "type": "Extension/HubsExtension/PartType/MarkdownPart",
                        "settings": {
                            "content": {
                                "settings": {
                                    "title": "App Service Plan",
                                    "subtitle": "",
                                    "content": "Avg CPU and Memory Percentage",
                                    "markdownSource": 1,
                                    "markdownUri": null
                                }
                            }
                        }
                    }
                },
                "5": {
                    "position": {
                        "x": 2,
                        "y": 2,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-cohortdistributionorchplan"
                                                },
                                                "name": "CpuPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "CPU Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-cohortdistributionorchplan"
                                                }
                                            },
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-cohortdistributionorchplan"
                                                },
                                                "name": "MemoryPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "Memory Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-cohortdistributionorchplan"
                                                }
                                            }
                                        ],
                                        "title": "cohortdistributionorchplan",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideHoverCard": false,
                                                "hideLabelNames": true
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "6": {
                    "position": {
                        "x": 5,
                        "y": 2,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-cohortdistributionplan"
                                                },
                                                "name": "CpuPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "CPU Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-cohortdistributionplan"
                                                }
                                            },
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-cohortdistributionplan"
                                                },
                                                "name": "MemoryPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "Memory Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-cohortdistributionplan"
                                                }
                                            }
                                        ],
                                        "title": "cohortdistributionplan",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideHoverCard": false,
                                                "hideLabelNames": true
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "7": {
                    "position": {
                        "x": 8,
                        "y": 2,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-nonscaling"
                                                },
                                                "name": "CpuPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "CPU Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-nonscaling"
                                                }
                                            },
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-nonscaling"
                                                },
                                                "name": "MemoryPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "Memory Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-nonscaling"
                                                }
                                            }
                                        ],
                                        "title": "nonscaling",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideHoverCard": false,
                                                "hideLabelNames": true
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "8": {
                    "position": {
                        "x": 2,
                        "y": 4,
                        "colSpan": 3,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "chartType",
                                "isOptional": true
                            },
                            {
                                "name": "isShared",
                                "isOptional": true
                            },
                            {
                                "name": "queryId",
                                "isOptional": true
                            },
                            {
                                "name": "formatResults",
                                "isOptional": true
                            },
                            {
                                "name": "partTitle",
                                "value": "Query 1",
                                "isOptional": true
                            },
                            {
                                "name": "queryScope",
                                "value": {
                                    "scope": 0,
                                    "values": []
                                },
                                "isOptional": true
                            },
                            {
                                "name": "query",
                                "value": "Resources\n| where type == \"microsoft.web/serverfarms\" and resourceGroup == \"${core_resource_group}\" and name == \"${core_app_service_plan}-cohortdistributionorchplan\"\n| extend InstanceCount = toint(sku.capacity)\n| summarize TotalInstances = sum(InstanceCount) by name\n| project [\"Instance Count\"] = TotalInstances",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/ArgQuerySingleValueTile",
                        "settings": {}
                    }
                },
                "9": {
                    "position": {
                        "x": 5,
                        "y": 4,
                        "colSpan": 3,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "chartType",
                                "isOptional": true
                            },
                            {
                                "name": "isShared",
                                "isOptional": true
                            },
                            {
                                "name": "queryId",
                                "isOptional": true
                            },
                            {
                                "name": "formatResults",
                                "isOptional": true
                            },
                            {
                                "name": "partTitle",
                                "value": "Query 1",
                                "isOptional": true
                            },
                            {
                                "name": "queryScope",
                                "value": {
                                    "scope": 0,
                                    "values": []
                                },
                                "isOptional": true
                            },
                            {
                                "name": "query",
                                "value": "Resources\n| where type == \"microsoft.web/serverfarms\" and resourceGroup == \"${core_resource_group}\" and name == \"${core_app_service_plan}-cohortdistributionplan\"\n| extend InstanceCount = toint(sku.capacity)\n| summarize TotalInstances = sum(InstanceCount) by name\n| project [\"Instance Count\"] = TotalInstances",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/ArgQuerySingleValueTile",
                        "settings": {}
                    }
                },
                "10": {
                    "position": {
                        "x": 8,
                        "y": 4,
                        "colSpan": 3,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "chartType",
                                "isOptional": true
                            },
                            {
                                "name": "isShared",
                                "isOptional": true
                            },
                            {
                                "name": "queryId",
                                "isOptional": true
                            },
                            {
                                "name": "formatResults",
                                "isOptional": true
                            },
                            {
                                "name": "partTitle",
                                "value": "Query 1",
                                "isOptional": true
                            },
                            {
                                "name": "queryScope",
                                "value": {
                                    "scope": 0,
                                    "values": []
                                },
                                "isOptional": true
                            },
                            {
                                "name": "query",
                                "value": "Resources\n| where type == \"microsoft.web/serverfarms\" and resourceGroup == \"${core_resource_group}\" and name == \"${core_app_service_plan}-nonscaling\"\n| extend InstanceCount = toint(sku.capacity)\n| summarize TotalInstances = sum(InstanceCount) by name\n| project [\"Instance Count\"] = TotalInstances",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/ArgQuerySingleValueTile",
                        "settings": {}
                    }
                },
                "11": {
                    "position": {
                        "x": 2,
                        "y": 5,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-participantmanagementorchplan"
                                                },
                                                "name": "CpuPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "CPU Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-participantmanagementorchplan"
                                                }
                                            },
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-participantmanagementorchplan"
                                                },
                                                "name": "MemoryPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "Memory Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-participantmanagementorchplan"
                                                }
                                            }
                                        ],
                                        "title": "participantmanagementorchplan",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideHoverCard": false,
                                                "hideLabelNames": true
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "12": {
                    "position": {
                        "x": 5,
                        "y": 5,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-participantmanagementplan"
                                                },
                                                "name": "CpuPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "CPU Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-participantmanagementplan"
                                                }
                                            },
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-participantmanagementplan"
                                                },
                                                "name": "MemoryPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "Memory Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-participantmanagementplan"
                                                }
                                            }
                                        ],
                                        "title": "participantmanagementplan",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideHoverCard": false,
                                                "hideLabelNames": true
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "13": {
                    "position": {
                        "x": 8,
                        "y": 5,
                        "colSpan": 3,
                        "rowSpan": 2
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "options",
                                "isOptional": true
                            },
                            {
                                "name": "sharedTimeRange",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/MonitorChartPart",
                        "settings": {
                            "content": {
                                "options": {
                                    "chart": {
                                        "metrics": [
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-tarpitplan"
                                                },
                                                "name": "CpuPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "CPU Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-tarpitplan"
                                                }
                                            },
                                            {
                                                "resourceMetadata": {
                                                    "id": "/subscriptions/${core_sub_id}/resourceGroups/${core_resource_group}/providers/Microsoft.Web/serverFarms/${core_app_service_plan}-tarpitplan"
                                                },
                                                "name": "MemoryPercentage",
                                                "aggregationType": 4,
                                                "namespace": "microsoft.web/serverfarms",
                                                "metricVisualization": {
                                                    "displayName": "Memory Percentage",
                                                    "resourceDisplayName": "${core_app_service_plan}-tarpitplan"
                                                }
                                            }
                                        ],
                                        "title": "tarpitplan",
                                        "titleKind": 1,
                                        "visualization": {
                                            "chartType": 2,
                                            "legendVisualization": {
                                                "isVisible": true,
                                                "position": 2,
                                                "hideHoverCard": false,
                                                "hideLabelNames": true
                                            },
                                            "axisVisualization": {
                                                "x": {
                                                    "isVisible": true,
                                                    "axisType": 2
                                                },
                                                "y": {
                                                    "isVisible": true,
                                                    "axisType": 1
                                                }
                                            },
                                            "disablePinning": true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "14": {
                    "position": {
                        "x": 2,
                        "y": 7,
                        "colSpan": 3,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "chartType",
                                "isOptional": true
                            },
                            {
                                "name": "isShared",
                                "isOptional": true
                            },
                            {
                                "name": "queryId",
                                "isOptional": true
                            },
                            {
                                "name": "formatResults",
                                "isOptional": true
                            },
                            {
                                "name": "partTitle",
                                "value": "Query 1",
                                "isOptional": true
                            },
                            {
                                "name": "queryScope",
                                "value": {
                                    "scope": 0,
                                    "values": []
                                },
                                "isOptional": true
                            },
                            {
                                "name": "query",
                                "value": "Resources\n| where type == \"microsoft.web/serverfarms\" and resourceGroup == \"${core_resource_group}\" and name == \"${core_app_service_plan}-participantmanagementorchplan\"\n| extend InstanceCount = toint(sku.capacity)\n| summarize TotalInstances = sum(InstanceCount) by name\n| project [\"Instance Count\"] = TotalInstances",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/ArgQuerySingleValueTile",
                        "settings": {}
                    }
                },
                "15": {
                    "position": {
                        "x": 5,
                        "y": 7,
                        "colSpan": 3,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "chartType",
                                "isOptional": true
                            },
                            {
                                "name": "isShared",
                                "isOptional": true
                            },
                            {
                                "name": "queryId",
                                "isOptional": true
                            },
                            {
                                "name": "formatResults",
                                "isOptional": true
                            },
                            {
                                "name": "partTitle",
                                "value": "Query 1",
                                "isOptional": true
                            },
                            {
                                "name": "queryScope",
                                "value": {
                                    "scope": 0,
                                    "values": []
                                },
                                "isOptional": true
                            },
                            {
                                "name": "query",
                                "value": "Resources\n| where type == \"microsoft.web/serverfarms\" and resourceGroup == \"${core_resource_group}\" and name == \"${core_app_service_plan}-participantmanagementplan\"\n| extend InstanceCount = toint(sku.capacity)\n| summarize TotalInstances = sum(InstanceCount) by name\n| project [\"Instance Count\"] = TotalInstances",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/ArgQuerySingleValueTile",
                        "settings": {}
                    }
                },
                "16": {
                    "position": {
                        "x": 8,
                        "y": 7,
                        "colSpan": 3,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "chartType",
                                "isOptional": true
                            },
                            {
                                "name": "isShared",
                                "isOptional": true
                            },
                            {
                                "name": "queryId",
                                "isOptional": true
                            },
                            {
                                "name": "formatResults",
                                "isOptional": true
                            },
                            {
                                "name": "partTitle",
                                "value": "Query 1",
                                "isOptional": true
                            },
                            {
                                "name": "queryScope",
                                "value": {
                                    "scope": 0,
                                    "values": []
                                },
                                "isOptional": true
                            },
                            {
                                "name": "query",
                                "value": "Resources\n| where type == \"microsoft.web/serverfarms\" and resourceGroup == \"${core_resource_group}\" and name == \"${core_app_service_plan}-tarpitplan\"\n| extend InstanceCount = toint(sku.capacity)\n| summarize TotalInstances = sum(InstanceCount) by name\n| project [\"Instance Count\"] = TotalInstances",
                                "isOptional": true
                            }
                        ],
                        "type": "Extension/HubsExtension/PartType/ArgQuerySingleValueTile",
                        "settings": {}
                    }
                }
            }
        }
    }
}