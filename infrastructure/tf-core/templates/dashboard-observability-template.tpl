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
                                    "content": "### Application Performance",
                                    "title": "",
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
                "2": {
                    "position": {
                        "x": 4,
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
                "3": {
                    "position": {
                        "x": 7,
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
                "4": {
                    "position": {
                        "x": 10,
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
                "5": {
                    "position": {
                        "x": 13,
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
                                                "name": "requests/count",
                                                "aggregationType": 7,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "resourceDisplayName": "${audit_resource_name_app_insights}",
                                                    "color": "#0078D4"
                                                }
                                            }
                                        ],
                                        "title": "Server requests",
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
                                                    "menuid": "performance"
                                                },
                                                "extensionName": "HubsExtension",
                                                "options": {
                                                    "parameters": {
                                                        "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                        "menuid": "performance"
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
                                                "name": "requests/count",
                                                "aggregationType": 7,
                                                "namespace": "microsoft.insights/components",
                                                "metricVisualization": {
                                                    "resourceDisplayName": "${audit_resource_name_app_insights}",
                                                    "color": "#0078D4"
                                                }
                                            }
                                        ],
                                        "title": "Server requests",
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
                                                    "menuid": "performance"
                                                },
                                                "extensionName": "HubsExtension",
                                                "options": {
                                                    "parameters": {
                                                        "id": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/Microsoft.Insights/components/${audit_resource_name_app_insights}",
                                                        "menuid": "performance"
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
        }
    }
}