Context Sensitive Spelling Corrector
------------------------------------

This solution can simply be opened in any version of Visual Studio and the
target machine must have the .NET Framework 4.5 or above installed.

The solution is organized into modular projects, each of which handles a
different part of the application.

The project is organized as follows:

- ConsoleClient:

    This is the project that builds the automated training and testing
    application program. It depends on the core libraries for the main
    functionality but is responsible for running all the tests and reporting
    the final accuracy. It is very CPU intensive and takes around 2 hours to
    train on most recent laptops and about 40 to 50 minutes on any reasonable
    desktop machine. After the training has been done, it starts running the
    tests to find average accuracy which normally takes under an hour. To
    improve the speed of the application, data serialization is used in 3
    different parts to try and reuse as much computation as possible. The
    preprocessed corpus, parts-of-speech tagged sentences and the previously
    trained models are all serialized to XML files using DataContract.

- ContextSensitiveSpellingCorrection

    This is the project that is the central point for the Context Corrector and
    has helper functions to interface with the lower level library code. It
    provides functions to preprocess the corpus, perform parts-of-speech
    tagging on training data, performing parallel online training of models and
    testing of the trained models.

- Corpus

    This directory holds the different corpora we are using. The "Release
    Corpus.txt", "Debug Corpus.txt", "Test Corpus.txt" are used by the context
    corrector for training and testing. The client machine doesn't need this
    corpus after a model has been trained. "big.txt" contains a dictionary
    which is used to drive the EditCorrector.

- EditCorrector

    This project is an implementation of an edit-distance based spelling
    corrector which uses a dictionary to find the word with minimum edit
    distance from the target word and presents that as a correction.

- FeatureExtractor

    This project provides methods that extract the collocations and context
    words from training data and feed it to the Winnow project to train on.
    This project can be extended with additional classes to add support for
    other kind of features.

- FeatureSelector

    This class performs feature pruning and a Chi-Square test to determine
    which features to actually use for training and creating the model.

- Models

    This directory contains the trained model ("Trained.xml"), the output of
    the preprocessing and parts-of-speech tagging of the training corpus
    ("Sentence.xml"). It also includes a parts-of-speech tag dictionary
    ("tagdict") needed by the parts-of-speech tagger at runtime to be able to
    perform parts-of-speech tagging. The ".nbin" files are additional trained
    models for the parts-of-speech tagger that can be swapped out easier and
    faster corpus preprocessing times.

- packages

    This directory holds the external libraries which in our case is managed
    through the nuget package manager. We are directly dependant on OpenNLP
    only for the Windows applications. The other libraries are used for the
    WebAPIServer and WebAPIClient.

- REPLApplication

    This project provides a console application that runs a "REPL" (Read, Eval,
    Print Loop) that waits for user input and performs either Context
    Correction (ContextSensitiveSpellingCorrection), Edit Distance Correction
    (EditCorrector) or a combination of the two depending on the values in it's
    config file present at "REPLApplication/app.config". The modular structure
    also allows it to hook into any other correction providers. The correction
    providers take a sentence or paragraph as an input and return a data
    structure that maps the index of the corrected word in the original string
    to the correction to be performed. The REPLApplication uses that
    information to build the final corrected string with a delta of the
    corrections that were made and by which corrector.

- WebAPIClient

    This project provides a console application that talks to a web API server
    (WebAPIServer) and sends it sentences to be corrected and reads the
    response from the server in either a JSON encoded or XML format. This is
    basically a version of the REPLApplication that can talk over TCP/IP or
    WebSockets.

- WebAPIServer

    This project provides a web application that exposes an API accessible from
    TCP/IP or over WebSockets. It implements all the functions that the
    REPLApplication implements and provides most of the functionality through
    the ContextSensitiveSpellingCorrection and EditCorrector assemblies.

- Winnow

    This project provides an array of classifiers for each confusible set, each
    of which are trained using the Winnow algorithm. It has 4 tunable
    parameters:

    1. The half-width used for finding context-words called k (which we have
    set to 10).

    2. The half-width used for finding collocations called l (which we have set
    to 2).

    3. The promotion factor for Winnow algorithm called alpha which we have set
    to 1.5.

    4. The demotion factor for Winnow algorithm called beta which we have set
    to 0.5.

- plotResults.m

    This is a MATLAB/Octave script which reads data from the output generated
    by the ConsoleClient from Results.csv and generates a clean sorted graph of
    confusion sets and the accuracies achieved on them.

--------------------------------------------------------------------------------

This modular structure means that any new corrector or classification algorithm
can be combined simply by creating a new class that can take a sentence as an
input and return the index of the corrected word and the correction to be
performed.
