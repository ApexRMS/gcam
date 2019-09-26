# gcam
SyncroSim Base Package providing an interface for the Global Change Assessment Model (GCAM)

## Overview

The [SyncroSim](https://syncrosim.com/) gcam package is designed to provide an interface for the [GCAM model](http://www.globalchange.umd.edu/gcam/). GCAM is a simulation model developed at PNW Laboratory and the University of Maryland. GCAM simulates the interactions between the global energy system, water, agriculture and land use, the economy and climate.  You can find out more about GCAM by viewing its [online documentation](http://jgcri.github.io/gcam-doc/).

The gcam package for SyncroSim allows users to structure  scenario input and output data, run the model and explore model outputs using either a graphical user interface (GUI), a command line interface or the [rsyncrosim](https://syncrosim.com/r-package/) package for [R](https://www.r-project.org/). The initial version of the gcam package is designed for running [GCAM-USA](https://jgcri.github.io/gcam-doc/gcam-usa.html). This initial version allows users to select a GCAM configuration xml file (for example, configuration_usa.xml) and a policy target xml file (for example, forcing_target_4p5.xml). SyncroSim gcam can invoke a GCAM simulation and when the simulation is complete, imports selected simulation output into a structured [SyncroSim library](http://docs.syncrosim.com/how_to_guides/library_overview.html).  The output can then be explored and analysed either through the [SyncroSim GUI](http://docs.syncrosim.com/how_to_guides/results_overview.html), through the [command line interface](http://docs.syncrosim.com/reference/console.html), or through the [rsyncrosim package](https://syncrosim.com/r-package/).  The initial version of the SyncroSim gcam package focuses on exploration of the [detailed land allocation output](http://jgcri.github.io/gcam-doc/aglu.html). In this version, output for detailed land allocation can be viewed, and disaggregated by Region, Land Use, Irrigation Type and Intensity.

## Installation Instructions

1.  [Install SyncroSim](http://docs.syncrosim.com/getting_started/inst_win.html).
2. Install the gcam package ([example of how to install a package](http://docs.syncrosim.com/getting_started/quickstart.html#step-1---install-the-demo-sales-package)).
3. Download a [GCAM model release](https://github.com/JGCRI/gcam-core/releases) (The initial version of gcam was developed against [GCAM for windows 5.1.3](https://github.com/JGCRI/gcam-core/releases/tag/gcam-v5.1.3)). Extract the contents of the downloaded zip file to a local folder on your computer.
4. Note the **GCAM Application Directory** is the location where you extract the contents of the GCAM model release that you downloaded in step 3. You will need to specify this in your [SyncroSim library properties](http://docs.syncrosim.com/how_to_guides/properties_overview.html) for GCAM.

## Getting Started

### Configure Library Properties

1. Open the SyncroSim GUI and select **File | New**.
2. Select the **gcam** base package and the **GCAM USA** template; specify a file name and folder location for  your gcam SyncroSim library and click **OK**.
3. The [SyncroSim library explorer](http://docs.syncrosim.com/how_to_guides/library_explorer_overview.html) should now display your new gcam library.
4. Highlight your gcam library in the library explorer and select **File | Library Properties**.
5. On the **GCAM tab** of the **Library Properties** specify the location of your **GCAM Application Directory** where you extracted the GCAM release that you downloaded (See **Installation Steps 3 and 4**).
6. In the **Library Properties** for GCAM you can also select to **Run in window** which will at runtime show the console for GCAM and give you updates on the progress of your simulation.  Note that if you choose this option, when a simulation is complete you will be prompted twice to **click any key to continue**.

### View Definitions

1. In the [SyncroSim library explorer](http://docs.syncrosim.com/how_to_guides/library_explorer_overview.html) right click on **Definitions** and select **Properties**.
2. The GCAM USA template library has been preloaded with the GCAM-USA Regions, Land Allocations, Irrigation Types (irrigated-IRR or rainfed-RFD), and Intensities (high or low). Scroll through the different tabs for **Definitions** to view the different values specified for Regions and Land Allocations.

### View Scenario Properties

1. In the [SyncroSim library explorer](http://docs.syncrosim.com/how_to_guides/library_explorer_overview.html) right click on the scenario called **Configuration USA Forcing Target 4p5** and select **Properties**.
2. On the **Input Files** tab note that you can specify a **Configuration File** and a **Policy Target File**.  If no file is selected the defaults are shown on this screen (configuration_usa.xml and forcing_target_4p5.xml).
3. Future versions of gcam may expose additional input file options and other model settings.

### Run the GCAM Model

1. In the [SyncroSim library explorer](http://docs.syncrosim.com/how_to_guides/library_explorer_overview.html) right click on the scenario called **Configuration USA Forcing Target 4p5** and select **Run**.
2. If you checked **Run in window** in the **GCAM Library Properties** you will see the GCAM console open and show the progress of the simulation.  When the simulation is complete you will be prompted to press any key to continue and once again a second console window will open that queries the detailed land allocation output and imports it into your SyncroSim library. You will then be prompted again to press any key to continue.

### View Charts of Output

Once the simulation is complete you can use the [SyncroSim chart window](http://docs.syncrosim.com/how_to_guides/results_chart_window.html) to view GCAM outputs. Note that you can disaggregate and include output for Region, Land Allocation, Irrigation and Intensity. Note also that because GCAM does not produce output for every single year in a simulation we recommend that under Chart Options you de-select **No data as zero** and select **Show data points**. An example chart for **California and PNW Forests** is provided.

You can also choose to use the [rsyncrosim package](https://syncrosim.com/r-package/) to extract model output into R dataframes.