# ComicBookGenerator API


## Project Description
This is the API used for all of the actions associated with the StoryTimeComicBook App (see Additional Dependencies for location)
A user should be able to create their very own comic book with handdrawn images (or any image), and a story to create scenes. Then a full pdf comic book is created generating new images and a story based on user inputs.

A voice model trainer is also available for the user to submit audio snippets of their voice, however, the training of this model on Replicate is not fully completed at this time (3/13/25)

A to-do will be for the fine tuned audio model to read the story back to you while you view the finished comic.


## Additional Dependencies
This project is designed to only be run locally with the StoryTimeComicBook Angular UI (https://github.com/rroethle7474/comic-book-generator). Please review the README associated with that before running.

Both a Replicate and LLM API key (OpenAI, Anthropic, Gemini) in order to properly generate the images, train the voice model, and create the story and add to the appsettings.JSON file.

All files are saved on the user's local machine. (please see the wwwroot folders)

A postgreSQL database is also required for local development. If postGreSQL is not installed, please see the following link for installation instructions: https://www.postgresql.org/download/.

Update the 'ConnectionStrings' setting in the appsettings.json file to point to your local database. The database will be created automatically when the application is run for the first time.