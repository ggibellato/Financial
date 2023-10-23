provider "aws" {
  
}

module "s3" {
   source = "./modules/s3"
   bucket_name = "financialdata"
}