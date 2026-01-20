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
                        "inputs": [
                            {
                                "name": "id",
                                "value": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/microsoft.insights/components/${audit_resource_name_app_insights}"
                            },
                            {
                                "name": "Version",
                                "value": "1.0"
                            }
                        ],
                        "type": "Extension/AppInsightsExtension/PartType/AspNetOverviewPinnedPart",
                        "asset": {
                            "idInputName": "id",
                            "type": "ApplicationInsights"
                        },
                        "defaultMenuItemId": "overview"
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
                }
            }
        },
        "1": {
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
                                    "content": "App Service Plan",
                                    "title": "Avg CPU and Memory Percentage",
                                    "subtitle": "",
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
                                                    "resourceDisplayName": "asp-sbrk-uks-cohman-cohortdistributionorchplan"
                                                }
                                            }
                                        ],
                                        "title": "${core_app_service_plan}-cohortdistributionorchplan",
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
                }
            }
        }
    }
}