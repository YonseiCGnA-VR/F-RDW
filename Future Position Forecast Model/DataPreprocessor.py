import pandas as pd
import utils
import os
import glob
import numpy as np


outputFilePath = "./preprocessedData"
dataFilePath = "./data"
fileExtension = ".txt"

try:
    os.path.isdir(outputFilePath)
except:
    try:
        os.mkdir(outputFilePath)
    except:

        print("path : " + outputFilePath + " mkdir Error ...")
        exit(-1)
try:
    os.path.isdir(dataFilePath)
except:
    print("path : " + dataFilePath + " doesn't exist ... ")
    exit(-1)

dataSet = np.empty(0)
dataBuilder = utils.SingleDataBuilder("")
for filename in glob.glob(dataFilePath + '/*' + fileExtension):
    print("filename : " + filename)


    dataBuilder.setFile(filename)
    dataSource = dataBuilder.getDatas()
    data = dataSource[:,7:]

    df = pd.DataFrame(data)
    fName = os.path.basename(filename).rstrip(".txt")
    df.to_csv(outputFilePath + "/" + fName + ".csv", index = None, header=None)



