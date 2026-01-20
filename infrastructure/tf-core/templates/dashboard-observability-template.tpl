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
                        "colSpan": 3,
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
                        "x": 3,
                        "y": 0,
                        "colSpan": 5,
                        "rowSpan": 3
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
                                                    "resourceDisplayName": "appi-sbrk-uks-cohman",
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
                }
            }
        }
    }
}