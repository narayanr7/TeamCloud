# coding=utf-8
# --------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator.
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.
# --------------------------------------------------------------------------

from msrest.serialization import Model


class TeamCloudConfiguration(Model):
    """TeamCloudConfiguration.

    :param project_types:
    :type project_types: list[~teamcloud.models.ProjectType]
    :param providers:
    :type providers: list[~teamcloud.models.Provider]
    :param users:
    :type users: list[~teamcloud.models.User]
    :param tags:
    :type tags: dict[str, str]
    :param properties:
    :type properties: dict[str, str]
    """

    _attribute_map = {
        'project_types': {'key': 'projectTypes', 'type': '[ProjectType]'},
        'providers': {'key': 'providers', 'type': '[Provider]'},
        'users': {'key': 'users', 'type': '[User]'},
        'tags': {'key': 'tags', 'type': '{str}'},
        'properties': {'key': 'properties', 'type': '{str}'},
    }

    def __init__(self, *, project_types=None, providers=None, users=None, tags=None, properties=None, **kwargs) -> None:
        super(TeamCloudConfiguration, self).__init__(**kwargs)
        self.project_types = project_types
        self.providers = providers
        self.users = users
        self.tags = tags
        self.properties = properties
