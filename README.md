# CLRCompanion

A discord bot to put OpenAIs in your Discord server, written in .NET 7

It can simulate being multiple bots at once with Discord webhooks, and can be configured to use different models for each bot.

There is also an unfinished web UI to manage the bots.

## Features

- Add one or more bots to a Discord channel
- Bots can be configured with the following:
	- Name
	- Prompt
	- Model
	- Chance to reply
	- History it can view
	- an avatar url
	- whether it is the default for a channel
- Users can reply to bots.

## Setup

1. Install .NET 7
2. Clone the repo
3. Create a Discord bot and get its token
4. Add the bot to your server -- make sure it has webhook permissions
5. Create a file called `.env` and follow the `.env.example` file to fill it out
6. Run `dotnet run` in the project directory

## Usage

Adding and managing bots is done via slash commands. The following commands are available:

 - `/add` - Add a bot to the channel
 - `/delete` - Remove a bot from the channel
 - `/list` - List the bots in the channel
 - `/config` - Gets the config for a bot
 - `/prompt` - Gets the prompt for a bot
 - `/edit` - Set the default bot for the channel
 - `/ask` - Asks a bot a question
 - `/ping` - Debug command to check if the bot is running

## Roadmap

 - Finish the Web UI
 - Add more options to the bots (temperature, max tokens, etc)
 - More advanced prompt templates e.g. adding channel name into the prompt
 - Character.ai integration