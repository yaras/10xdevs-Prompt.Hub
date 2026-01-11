# Application: PromptHub - AI Prompt Storage and Sharing Platform

## Short description

The purpose of that application is to provide users with a platform to store, share, and collaborate on AI prompts. The application should be designed with simplicity, short time to market and basic, MVP features at first.

## Minimum features

I want users to be able to:
- Login using OAuth (Outlook, Google, e-mail)
- Create, read, update, delete prompts
- Mark prompts as private or public
- Categorize prompts with tags
- Search prompts by keywords and tags
- Add likes or dislikes to prompts

## Prompt structure

A single entry for a prompt contains:
- A title
- The prompt text
- Tags
- Author email (optional; stored for display in lists)

## Additional information

- I want to store prompts in the Azure Table Storage for simplicity.
- I want to use AI to suggest tags based on the prompt text when users create or update prompts.

## What is not included

- Advanced collaboration features (e.g., real-time editing)
- Commenting
- Versioning of prompts

## Succcess criteria

- The application is deployed and accessible via a web browser.
- Users can successfully login, and manage their prompts.
- The application should be responsive and work well on both desktop and mobile devices.
- The application should have a clean and user-friendly interface.
- 75% of users should be able to find and use the main features without assistance.