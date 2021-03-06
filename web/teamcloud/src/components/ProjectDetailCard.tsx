// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from 'react';
import { Stack, IStackStyles, ITextStyles, getTheme, FontWeights, Text } from '@fluentui/react';

export interface IProjectDetailCardProps {
    title?: string;
    callout?: string;
}

export const ProjectDetailCard: React.FunctionComponent<IProjectDetailCardProps> = (props) => {

    const theme = getTheme();

    const _cardStackStyles: IStackStyles = {
        root: {
            width: '100%',
            margin: '8px',
            padding: '20px 0',
            bordeRadius: theme.effects.roundedCorner2,
            boxShadow: theme.effects.elevation4
        }
    }

    const _cardStackContentStyles: IStackStyles = {
        root: {
            padding: '0 20px',
        }
    }

    const _titleStyles: ITextStyles = {
        root: {
            fontSize: '21px',
            fontWeight: FontWeights.semibold,
            marginBottom: '12px'
        }
    }

    const _calloutStyles: ITextStyles = {
        root: {
            fontSize: '13px',
            fontWeight: FontWeights.regular,
            color: 'rgb(102, 102, 102)',
            backgroundColor: theme.palette.neutralLighter,
            marginBottom: '14px',
            marginTop: '5px',
            padding: '2px 12px',
            borderRadius: '14px',
        }
    }

    const _getCallout = (): JSX.Element | null => props.callout ? <Text styles={_calloutStyles}>{props.callout}</Text> : null;

    const _getTitle = (): JSX.Element | null => props.title ? <Text styles={_titleStyles}>{props.title}</Text> : null;

    return (
        <Stack verticalFill styles={_cardStackStyles}>
            <Stack styles={_cardStackContentStyles} >
                <Stack horizontal tokens={{ childrenGap: '5px' }}>{_getTitle()}{_getCallout()}</Stack>
                {/* {_getTitle()} */}
                {props.children}
            </Stack>
        </Stack>
    );
}
